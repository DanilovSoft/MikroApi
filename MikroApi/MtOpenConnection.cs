﻿using DanilovSoft.MikroApi.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using DanilovSoft.MikroApi.Helpers;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MikroApi
{
    internal sealed class MtOpenConnection : IDisposable
    {
        /// <summary>
        /// Потокобезопасный словарь подписчиков на ответы сервера с разными тегами.
        /// </summary>
        internal readonly ResponseListeners _listeners = new();
        /// <summary>
        /// Основная очередь. В неё помещаются ответы сервера которые не маркированы тегом.
        /// </summary>
        private readonly ListenerQueue<MikroTikResponse> _mainQueue = new();
        /// <summary>
        /// Коллекцие тегов, фреймы которых нужно получить от сервера.
        /// </summary>
        private readonly Dictionary<string, int> _framesToRead = new();
        /// <summary>
        /// Очередь сообщений на отправку.
        /// </summary>
        private readonly Encoding _encoding;
        private readonly Memory<byte> _readBuffer;
        private readonly Memory<byte> _sendBuffer;
        private readonly MikroTikConnection _connection;
        private readonly TcpClient _tcpClient;
        private readonly Stream _stream;
        private Sender? _sender;
        private SocketTimeout? _receiveTimeout;
        private SocketTimeout? _sendTimeout;
        /// <summary>
        /// Доступ только через блокировку <see cref="_framesToRead"/>.
        /// </summary>
        private bool _reading;
        private bool _socketDisposed;
        /// <summary>
        /// Исключение произошедшее в результате чтения или записи в сокет.
        /// Доступ только через блокировку <see cref="_framesToRead"/>.
        /// </summary>
        private Exception? _socketException;
        private bool _disposed;

        // ctor
        public MtOpenConnection(MikroTikConnection connection, TcpClient tcpClient, Stream stream)
        {
            _connection = connection;
            _encoding = connection._encoding;
            _tcpClient = tcpClient;

            //Stream stream = tcpClient.GetStream();
            _stream = stream;
            _receiveTimeout = new SocketTimeout(OnReceiveTimeout, connection.ReceiveTimeout);
            _sendTimeout = new SocketTimeout(OnSendTimeout, connection.SendTimeout);

            const int BufferSize = 4096;
            byte[] sendRecvBuffer = new byte[BufferSize * 2];
            _readBuffer = new Memory<byte>(sendRecvBuffer, 0, BufferSize);
            _sendBuffer = new Memory<byte>(sendRecvBuffer, BufferSize, BufferSize);

            _sender = new(this);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _sender?.Quit();
                _sender = null;
                CloseSocket();
                _receiveTimeout?.Dispose();
                _receiveTimeout = null;
                _sendTimeout?.Dispose();
                _sendTimeout = null;
            }
        }

        /// <summary>
        /// Создает новый Listener с уникальным тегом, добавляет в словарь но не отправляет запрос.
        /// </summary>
        /// <returns></returns>
        public MikroTikResponseListener AddListener()
        {
            CheckDisposed();

            // Создать уникальный tag.
            string tag = CreateUniqueTag();

            var listener = new MikroTikResponseListener(tag, this);

            // Добавить в словарь.
            _listeners.Add(tag, listener);

            return listener;
        }

        /// <summary>
        /// Потокобезопасно создает уникальный тег.
        /// </summary>
        public string CreateUniqueTag()
        {
            CheckDisposed();

            return _connection.CreateUniqueTag();
        }

        /// <summary>
        /// Потокобезопасно добавляет в очередь задание — получить фрейм с указанным тегом.
        /// </summary>
        /// <param name="tagToReceive">Тег сообщения которое нужно прочитать из сокета.</param>
        /// <exception cref="IOException">Обрыв соединения.</exception>
        /// <returns></returns>
        public bool AddTagToRead(string tagToReceive)
        {
            CheckDisposed();

            lock (_framesToRead)
            {
                // Если был обрыв соединения то продолжать нельзя.
                if (_socketException == null)
                {
                    // Если в словаре уже есть такой тег то увеличить ему счетчик.
                    if (!_framesToRead.TryGetValue(tagToReceive, out int count))
                    {
                        // Добавить работы потоку который сейчас читает из сокета.
                        _framesToRead.Add(tagToReceive, 1);
                    }
                    else
                    // Сюда чеще всего попадают пустые теги.
                    {
                        _framesToRead[tagToReceive] = count + 1;
                    }

                    if (!_reading)
                    {
                        _reading = true;
                        return true;
                    }
                }
                else
                {
                    throw _socketException;
                }
            }
            return false;
        }

        public MikroTikResponseFrameDictionary Listen(MikroTikResponseListener listener, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDisposed();

            // Взять из кэша или ждать поступления.
            if (!TryTakeBeforeListen(listener, out var frame))
            {
                return listener.Take(millisecondsTimeout, cancellationToken);
            }
            else
            {
                return frame;
            }
        }

        public ValueTask<MikroTikResponseFrameDictionary> ListenAsync(MikroTikResponseListener listener, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDisposed();

            // Взять из кэша или ждать поступления.
            if (!TryTakeBeforeListen(listener, out var frame))
            {
                return listener.TakeAsync(millisecondsTimeout, cancellationToken);
            }
            else
            {
                return new ValueTask<MikroTikResponseFrameDictionary>(frame);
            }
        }

        public Task StartRead()
        {
            CheckDisposed();

            // Читаем из сокета.
            return ReadUntilHasTagsToReadAsync();
        }

        public Task CancelListenersAsync(bool wait)
        {
            CheckDisposed();

            if (wait)
            {
                var cancelCommand = new MikroTikCommand("/cancel");

                // Отправляем команду и ждем ответ.
                return RequestAsync(cancelCommand);
            }
            else
            {
                // Что бы не происходило ожидание завершения команды 
                // нужно сделать её тегированной и не добавлять в словарь.

                string selfTag = CreateUniqueTag();
                var cancelAllcommand = new MikroTikCancelAllCommand(selfTag);

                // Отправка команды без ожидания ответа.
                return SendAsync(cancelAllcommand);
            }
        }

        public void CancelListeners(bool wait)
        {
            CheckDisposed();

            if (wait)
            {
                var cancelCommand = new MikroTikCommand("/cancel");

                // Отправляем команду и ждем ответ.
                SendRequest(cancelCommand);
            }
            else
            {
                string selfTag = CreateUniqueTag();
                var cancelAllcommand = new MikroTikCancelAllCommand(selfTag);

                // Отправка команды без ожидания ответа.
                Send(cancelAllcommand);
            }
        }

        /// <summary>
        /// Синхронная отправка запроса в сокет без получения ответа. Не проверяет переиспользование команды.
        /// </summary>
        public void Send(MikroTikCommand command)
        {
            CheckDisposed();

            _sender.Send(static (mt, s) => mt.SendCommandInLooper(s), command);
        }

        /// <summary>
        /// Асинхронная отправка запроса в сокет без получения ответа. Не проверяет переиспользование команды.
        /// </summary>
        public Task SendAsync(MikroTikCommand command)
        {
            CheckDisposed();

            return _sender.SendAsync(static (con, s) => con.SendCommandInLooperAsync(s), command);
        }

        /// <summary>
        /// Синхронная отправка запроса и получение ответа.
        /// </summary>
        public MikroTikResponse SendRequest(MikroTikCommand command)
        {
            CheckDisposed();

            bool readerIsStopped = AddTagToRead("");

            // Отправка команды в сокет через очередь.
            _sender.Send(static (mt, s) => mt.SendCommandInLooper(s), command);

            if (readerIsStopped)
            {
                // Запустить чтение из сокета.
                StartRead();
            }

            // Ожидает не тегированный ответ из сокета.
            return _mainQueue.Take();
        }

        /// <summary>
        /// Асинхронная отправка запроса в сокет и получение результата.
        /// </summary>
        public async Task<MikroTikResponse> SendAndGetResponseAsync(MikroTikCommand command)
        {
            CheckDisposed();

            command.CheckCompleted();

            // Отправка и получение ответа через очередь.
            MikroTikResponse result = await RequestAsync(command).ConfigureAwait(false);

            command.Completed();

            return result;
        }

        /// <summary>
        /// Синхронная отправка запроса в сокет и получение результата.
        /// </summary>
        public MikroTikResponse SendAndGetResponse(MikroTikCommand command)
        {
            CheckDisposed();

            command.CheckCompleted();

            // Отправка и получение ответа через очередь.
            var response = SendRequest(command);

            // Команду повторно использовать нельзя.
            command.Completed();

            return response;
        }

        /// <summary>
        /// Отправляет в сокет запрос на отмену не дожидаясь результата.
        /// </summary>
        /// <param name="tag">Тег связанной операции которую следует отменить.</param>
        public Task CancelListenerNoWaitAsync(string tag)
        {
            CheckDisposed();

            // Подписываемся на завершение отмены
            MikroTikCancelCommand cancelCommand = CreateCancelCommand(tag);

            // Отправить из другого потока.
            return _sender.SendAsync(static (mt, s) => mt.SendCommandInLooperAsync(s), cancelCommand);
        }

        /// <summary>
        /// Создает команду для отмены и добавляет в словарь.
        /// </summary>
        public MikroTikCancelCommand CreateCancelCommand(string tag)
        {
            CheckDisposed();

            string selfTag = CreateUniqueTag();
            var command = new MikroTikCancelCommand(tag, selfTag, this);

            // Подписываемся на завершение отмены.
            _listeners.Add(command.SelfTag, command);

            return command;
        }

        /// <summary>
        /// Сообщает серверу что выполняется разъединение.
        /// </summary>
        /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
        /// <exception cref="IOException">Обрыв соединения.</exception>
        public bool Quit(int millisecondsTimeout)
        {
            CheckDisposed();

            // Подписываемся на завершение отмены.
            var quitCommand = new MikroTikQuitCommand();

            // Добавляем в словарь.
            _listeners.AddQuit(quitCommand);

            // Добавить задание.
            bool readerIsStopped = AddTagToRead("");

            // Обязательно захватить блокировку перед отправкой.
            lock (quitCommand)
            {
                // Отправляем команду без ожидания ответа.
                Send(quitCommand);

                if (readerIsStopped)
                {
                    // Запустить чтение из сокета.
                    StartRead();
                }

                // Ждем получение !trap.
                return quitCommand.Wait(millisecondsTimeout);
            }
        }

        /// <summary>
        /// Сообщает серверу что выполняется разъединение.
        /// </summary>
        /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
        public async Task<bool> QuitAsync(int millisecondsTimeout)
        {
            CheckDisposed();

            var quitCommand = new MikroTikAsyncQuitCommand();

            // Добавляем в словарь.
            _listeners.AddQuit(quitCommand);

            bool readerIsStopped = AddTagToRead("");

            // Отправляем команду без ожидания ответа.
            await SendAsync(quitCommand).ConfigureAwait(false);

            if (readerIsStopped)
            {
                // Запустить чтение из сокета.
                StartRead();
            }

            // Ждем получение !trap.
            return await quitCommand.WaitAsync(millisecondsTimeout).ConfigureAwait(false);
        }

        /// <summary>
        /// Создает команду для отмены и добавляет в словарь.
        /// </summary>
        public MikroTikAsyncCancelCommand CreateAsyncCancelCommand(string tag)
        {
            CheckDisposed();

            string selfTag = CreateUniqueTag();
            var command = new MikroTikAsyncCancelCommand(tag, selfTag, this);

            // Подписываемся на завершение отмены.
            _listeners.Add(command.SelfTag, command);

            return command;
        }

        /// <summary>
        /// Удаляет подписчика из словаря. Потокобезопасно.
        /// </summary>
        public void RemoveListener(string tag)
        {
            CheckDisposed();

            _listeners.Remove(tag);
        }

        /// <summary>
        /// Возвращает фрейм из кэша или запускает чтение из сокета.
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="frame"></param>
        /// <exception cref="Exception"/>
        /// <returns></returns>
        private bool TryTakeBeforeListen(MikroTikResponseListener listener, [NotNullWhen(true)] out MikroTikResponseFrameDictionary? frame)
        {
            bool readerIsStopped;

            // Перед тем как проверить есть ли в кэше сообщения нужно заблокировать listener
            // что-бы читающий поток не мог добавить сообщение.
            lock (listener.SyncObj)
            {
                // Проверить результат в кэше.
                if (listener.TryTake(out frame))
                {
                    return true;
                }
                else
                // В кэше пусто, нужно ждать сообщение от сокета.
                {
                    // Добавить читающему потоку задачу на получение еще одного фрейма с таким тегом.
                    readerIsStopped = AddTagToRead(listener._tag);
                }
            }

            if (readerIsStopped)
            {
                StartRead();
            }

            return false;
        }

        // Обрыв соединения. Происходит при получении !fatal — означает что сервер закрывает соединение,
        // или при исключении в результате чтения из сокета.
        private void ConnectionClosed(Exception exception, bool gotFatal)
        {
            lock (_framesToRead)
            {
                // Отправляющий поток уже мог установить исключение.
                if (_socketException == null)
                {
                    _socketException = exception;
                    _framesToRead.Clear();
                }
                else
                {
                    exception = _socketException;
                }
                _reading = false;
            }

            // Сообщить всем подписчикам что произошел обрыв сокета.
            _listeners.AddCriticalException(exception, gotFatal);

            // Сообщить основному подписчику что произошел обрыв сокета.
            _mainQueue.AddCriticalException(exception);
        }

        /// <summary>
        /// Эта функция не генерирует исключения.
        /// </summary>
        private async Task ReadUntilHasTagsToReadAsync()
        {
            Debug.Assert(_receiveTimeout != null);

            try
            {
            ReadNextFrame:

                // если получен !trap
                bool trap = false;
                // если получен !done
                bool done = false;
                // если получен !fatal
                bool fatal = false;
                // если получен .tag=
                string? tag = null;
                string? fatalMessage = null;
                var fullResponse = new MikroTikResponse();
                var frame = new MikroTikResponseFrameDictionary();
                int count;

                _receiveTimeout.Start();
                try
                {
                    while (true)
                    // Читаем фреймы.
                    {
                        // Чтение заголовка строки.
                        await _stream.ReadBlockAsync(_readBuffer.Slice(0, 1)).ConfigureAwait(false);

                        if (_readBuffer.Span[0] == 0)
                        // Конец сообщения.
                        {
                            if (tag != null)
                            // Получен фрейм сообщения маркированный тегом.
                            {
                                if (_listeners.TryGetValue(tag, out var listener))
                                {
                                    lock (listener.SyncObj)
                                    {
                                        if (done)
                                        {
                                            // Работа с этим подписчиком завершена.
                                            listener.Done();
                                        }
                                        else
                                        {
                                            if (!trap)
                                            {
                                                listener.AddResult(frame);
                                            }
                                            else
                                            {
                                                listener.AddTrap(new MikroApiTrapException(frame));
                                            }
                                        }
                                    }
                                }

                                if (TryExit(tag))
                                {
                                    // Завершение потока.
                                    return;
                                }
                                else
                                {
                                    goto ReadNextFrame;
                                }
                            }
                            else
                            // Получен фрейм сообщения, он не маркирован тегом.
                            {
                                // fatal can be received only in cases when API is closing connection.
                                // fatal может быть только не тегированным.
                                if (fatal)
                                // Сервер закрывает соединение.
                                {
                                    var exception = new MikroApiFatalException(fatalMessage!);

                                    ConnectionClosed(exception, gotFatal: true);

                                    // Завершение потока. Текущий экземпляр MikroTikSocket больше использовать нельзя.
                                    return;
                                }

                                if (frame.Count > 0)
                                {
                                    fullResponse.Add(frame);
                                }

                                if (trap)
                                {
                                    var trapException = new MikroApiTrapException(fullResponse[0]);
                                    _mainQueue.AddTrap(trapException);
                                }

                                if (done)
                                // Сообщение получено полностью.
                                {
                                    // Положить в основную очередь.
                                    _mainQueue.AddResult(fullResponse);

                                    if (TryExit(""))
                                    {
                                        // Завершение потока.
                                        return;
                                    }
                                    else
                                    {
                                        goto ReadNextFrame;
                                    }
                                }
                            }

                            // Нельзя делать Clear потому что fullResponse.Add(frame).
                            frame = new MikroTikResponseFrameDictionary();

                            // Возвращаемся к чтению заголовка.
                            continue;
                        }
                        else
                        {
                            // Длина строки в байтах.
                            count = await GetSizeAsync(_readBuffer).ConfigureAwait(false);
                        }

                        Memory<byte> buf = _readBuffer;
                        if (count > _readBuffer.Length)
                        {
                            buf = new byte[count];
                        }

                        // Чтение строки.
                        await _stream.ReadBlockAsync(buf.Slice(0, count)).ConfigureAwait(false);

#if NETSTANDARD2_0
                        string word = _encoding.GetString(buf.Slice(0, count));

#else
                        string word = _encoding.GetString(buf.Slice(0, count).Span);
#endif

                        Debug.WriteLine(word);

                        if (word == "!re")
                        {
                            continue;
                        }

                        if (!done && word == "!done")
                        {
                            done = true;
                            continue;
                        }

                        if (!fatal && word == "!fatal")
                        {
                            fatal = true;
                            continue;
                        }

                        if (!trap && word == "!trap")
                        {
                            trap = true;
                            continue;
                        }

                        int pos = word.IndexOf('=', 1);
                        if (pos >= 0)
                        {
                            string key = word.Substring(1, pos - 1);
                            string value = word.Substring(pos + 1);

                            if (key == "tag")
                            // Этот ответ тегирован.
                            {
                                tag = value;
                            }
                            else
                            {
                                if (!frame.ContainsKey(key))
                                {
                                    frame.Add(key, value);
                                }
                            }
                        }
                        else if (fatal)
                        {
                            fatalMessage = word;
                        }
                    }
                }
                finally
                {
                    // Может бросить исключение.
                    _receiveTimeout.Stop();
                }
            }
            catch (Exception ex)
            // Произошел обрыв сокета.
            {
                ConnectionClosed(ex, gotFatal: false);

                // Завершение потока. Текущий экземпляр MikroTikSocket больше использовать нельзя.
                return;
            }

            bool TryExit(string receivedTag)
            // Считано сообщение или фрейм.
            {
                lock (_framesToRead)
                {
                    if (_framesToRead.TryGetValue(receivedTag, out int tagCount))
                    {
                        if (tagCount == 1)
                        {
                            // Прочитаны все фреймы с таким тегом.
                            _framesToRead.Remove(receivedTag);

                            if (_framesToRead.Count == 0)
                            // Все сообщения были прочитаны.
                            {
                                // Завершаем поток чтения.
                                _reading = false;
                                return true;
                            }
                        }
                        else
                        {
                            // Требуется продолжить читать фреймы для этого тега.
                            _framesToRead[receivedTag] = (tagCount - 1);
                        }
                    }
                    // Продолжить чтение сообщений.
                    return false;
                }
            }
        }

        /// <summary>
        /// Асинхронно отправляет запрос и получает ответ через основную очередь.
        /// </summary>
        private async Task<MikroTikResponse> RequestAsync(MikroTikCommand command)
        {
            Debug.Assert(_sender != null);

            bool readerIsStopped = AddTagToRead("");

            // Отправка команды в сокет.
            await _sender.SendAsync(static (mt, s) => mt.SendCommandInLooperAsync(s), command).ConfigureAwait(false);

            if (readerIsStopped)
            {
                // Запустить чтение из сокета.
                StartRead();
            }

            // Ожидает не тегированный ответ из сокета.
            return await _mainQueue.TakeAsync().ConfigureAwait(false);
        }

        private Memory<byte> Encode(MikroTikCommand command)
        {
            int offset = 0;

            // Учитываем Null-терминатор.
            int totalLength = 1;

            bool useSendBuffer = true;

            for (int i = 0; i < command._lines.Count; i++)
            {
                // Строка.
                string line = command._lines[i];

                // Длина строки.
                int lineBytesCount = _encoding.GetByteCount(line);

                totalLength += lineBytesCount;

                // Размер длины строки.
                int encodedLength = EncodeLengthCount((uint)lineBytesCount);

                totalLength += encodedLength;

                // Если влезет в буффер.
                if (totalLength <= _sendBuffer.Length)
                {
                    // Записать в буффер длину строки.
                    EncodeLength((uint)lineBytesCount, _sendBuffer.Slice(offset).Span);

                    offset += encodedLength;

#if NETSTANDARD2_0
                    // Записать в буффер строку.
                    _encoding.GetBytes(line, _sendBuffer.Slice(offset));
#else
                    // Записать в буффер строку.
                    _encoding.GetBytes(line, _sendBuffer.Slice(offset).Span);
#endif

                    offset += lineBytesCount;
                }
                else
                {
                    useSendBuffer = false;
                }
            }

            if (useSendBuffer)
            {
                _sendBuffer.Span[offset] = 0;
                return _sendBuffer.Slice(0, totalLength);
            }
            else
            {
                offset = 0;
                byte[] buffer = new byte[totalLength];
                for (int i = 0; i < command._lines.Count; i++)
                {
                    string line = command._lines[i];
                    int lineBytesCount = _encoding.GetByteCount(line);
                    offset += EncodeLength((uint)lineBytesCount, buffer);
                    offset += _encoding.GetBytes(line, 0, line.Length, buffer, offset);
                }
                return buffer;
            }
        }

        private static int EncodeLengthCount(uint len)
        {
            if (len < 128)
            {
                return 1;
            }

            if (len < 16384)
            {
                return 2;
            }

            if (len < 0x200000)
            {
                return 3;
            }

            if (len < 0x10000000)
            {
                return 4;
            }

            return 5;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="len">Размер блока в байтах.</param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static int EncodeLength(uint len, Span<byte> buffer)
        {
            if (len < 128)
            {
                buffer[0] = (byte)len;
                return 1;
            }
            if (len < 16384)
            {
                uint value = (len | 0x8000);
                buffer[0] = (byte)(value >> 8);
                buffer[1] = (byte)(value >> 16);
                return 2;
            }
            if (len < 0x200000)
            {
                uint value = (len | 0xC00000);
                buffer[0] = (byte)(value >> 16);
                buffer[1] = (byte)(value >> 8);
                buffer[2] = (byte)(value);
                return 3;
            }
            if (len < 0x10000000)
            {
                uint value = (len | 0xE0000000);
                buffer[0] = (byte)(value >> 24);
                buffer[1] = (byte)(value >> 16);
                buffer[2] = (byte)(value >> 8);
                buffer[3] = (byte)(value);
                return 4;
            }
            else
            {
                buffer[0] = 0xF0;
                buffer[1] = (byte)(len >> 24);
                buffer[2] = (byte)(len >> 16);
                buffer[3] = (byte)(len >> 8);
                buffer[4] = (byte)(len);
                return 5;
            }
        }

        #region Inside SendLooper

        /// <summary>
        /// Эта процедура должна вызываться только через отправляющую очередь <see cref="_sender"/>.
        /// Эту процедуру нужно вызывать через Send так как в ней не обрабатываются исключения
        /// </summary>
        private void SendCommandInLooper(MikroTikCommand command)
        {
            Debug.Assert(_sendTimeout != null);

            Debug.WriteLine(Environment.NewLine + command + Environment.NewLine);
            var buffer = Encode(command);

            try
            {
                using (_sendTimeout.Start())
                {
#if NETSTANDARD2_0
                    _stream.Write(buffer);
#else
                    _stream.Write(buffer.Span);
#endif
                }
            }
            catch (Exception ex)
            {
                OnSendExceptionInLooper(ex);
            }
        }

        /// <summary>
        /// Эта процедура должна вызываться только через отправляющую очередь <see cref="_sender"/>.
        /// Эту Процедура нужно вызывать через Send так как в ней не обрабатываются исключения.
        /// </summary>
        private async Task SendCommandInLooperAsync(object state)
        {
            Debug.Assert(_sendTimeout != null, "Проверили в public методе");

            var command = (MikroTikCommand)state;
            Debug.WriteLine(Environment.NewLine + command + Environment.NewLine);
            Memory<byte> buffer = Encode(command);

            try
            {
                using (_sendTimeout.Start())
                {
                    await _stream.WriteAsync(buffer).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                OnSendExceptionInLooper(ex);
            }
        }

        private void OnSendExceptionInLooper(Exception exception)
        {
            lock (_framesToRead)
            {
                // Поток читающий из сокета уже мог установить исключение.
                if (_socketException == null)
                {
                    _socketException = exception;
                    _framesToRead.Clear();
                }
                else
                {
                    // Взять более ранее исключение.
                    exception = _socketException;
                }
            }

            // Удалить всех подписчиков из словаря и запретить дальнейшее добавление.
            _listeners.AddCriticalException(exception, gotFatal: false);

            // Сообщить основному подписчику что произошел обрыв сокета.
            _mainQueue.AddCriticalException(exception);

            throw exception;
        }

        #endregion

        private async ValueTask<int> GetSizeAsync(Memory<byte> buffer)
        {
            if (buffer.Span[0] < 128)
            {
                return buffer.Span[0];
            }
            else if (buffer.Span[0] < 192)
            {
                await _stream.ReadBlockAsync(buffer.Slice(1, 1)).ConfigureAwait(false);

                int v = 0;
                for (int i = 0; i < 2; i++)
                {
                    v = (v << 8) + buffer.Span[i];
                }

                return v ^ 0x8000;
            }
            else if (buffer.Span[0] < 224)
            {
                await _stream.ReadBlockAsync(buffer.Slice(1, 2)).ConfigureAwait(false);

                int v = 0;
                for (int i = 0; i < 3; i++)
                {
                    v = (v << 8) + buffer.Span[i];
                }

                return v ^ 0xC00000;
            }
            else if (buffer.Span[0] < 240)
            {
                await _stream.ReadBlockAsync(buffer.Slice(1, 3)).ConfigureAwait(false);

                int v = 0;
                for (int i = 0; i < 4; i++)
                {
                    v = (v << 8) + buffer.Span[i];
                }

                return (int)(v ^ 0xE0000000);
            }
            else if (buffer.Span[0] == 240)
            {
                await _stream.ReadBlockAsync(buffer.Slice(0, 4)).ConfigureAwait(false);

                int v = 0;
                for (int i = 0; i < 4; i++)
                {
                    v = (v << 8) + buffer.Span[i];
                }

                return v;
            }
            else
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

#endif
                // Не должно быть такого.
                throw new MikroTikUnknownLengthException();
            }
        }

        private void OnReceiveTimeout(object? _)
        {
            lock (_framesToRead)
            {
                if (_socketException == null)
                {
                    _socketException = new MikroApiDisconnectException("Сокет был закрыт в связи с таймаутом чтения.");
                }
            }
            CloseSocket();
        }

        private void OnSendTimeout(object? _)
        {
            lock (_framesToRead)
            {
                if (_socketException == null)
                {
                    _socketException = new MikroApiDisconnectException("Сокет был закрыт в связи с таймаутом отправки.");
                }
            }
            CloseSocket();
        }

        /// <summary>
        /// Потокобезопасно закрывает сокет.
        /// </summary>
        private void CloseSocket()
        {
            lock (_stream)
            {
                // Этот stream мог уже быть уничтожен.
                if (!_socketDisposed)
                {
                    _stream.Dispose();
                    _tcpClient.Dispose();
                    _socketDisposed = true;
                }
            }
        }

        [MemberNotNull(nameof(_sender))]
        [MemberNotNull(nameof(_receiveTimeout))]
        [MemberNotNull(nameof(_sendTimeout))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (!_disposed)
            {
                Debug.Assert(_sender != null);
                Debug.Assert(_receiveTimeout != null);
                Debug.Assert(_sendTimeout != null);
                return;
            }
            ThrowHelper.ThrowConnectionDisposed();
        }
    }
}
