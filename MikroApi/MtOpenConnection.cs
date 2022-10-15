using DanilovSoft.MikroApi.Threading;
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
using System.Runtime.InteropServices;

namespace DanilovSoft.MikroApi;

internal sealed class MtOpenConnection : IDisposable
{
    /// <summary>
    /// Потокобезопасный словарь подписчиков на ответы сервера с разными тегами.
    /// </summary>
    internal readonly ResponseListeners _listeners = new();
    [SuppressMessage("Usage", "CA2213:Следует высвобождать высвобождаемые поля", Justification = "Мы не владеем этим объектом")]
    private readonly MikroTikConnection _connection;
    private readonly object _connectionSync = new();
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
    private readonly TcpClient _tcpClient;
    private readonly Stream _stream;
    private readonly SocketTimeout _receiveTimeout;
    private readonly SocketTimeout _sendTimeout;
    private Sender? _sender;
    private Task _backgroundReceive = Task.CompletedTask; // TODO возможно стоит await'ить перед Dispose.
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

    public MtOpenConnection(MikroTikConnection connection, TcpClient tcpClient, Stream stream)
    {
        _connection = connection;
        _encoding = connection._encoding;
        _tcpClient = tcpClient;
        _stream = stream;
        _receiveTimeout = new SocketTimeout(OnReceiveTimeout, connection.ReceiveTimeout);
        _sendTimeout = new SocketTimeout(OnSendTimeout, connection.SendTimeout);

        const int BufferSize = 4096;
        var sendRecvBuffer = new byte[BufferSize * 2];
        _readBuffer = new Memory<byte>(sendRecvBuffer, 0, BufferSize);
        _sendBuffer = new Memory<byte>(sendRecvBuffer, BufferSize, BufferSize);
        _sender = new(this);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            NullableHelper.SetNull(ref _sender)?.Quit(CancellationToken.None);
            CloseSocket();
            _receiveTimeout.Dispose();
            _sendTimeout.Dispose();
        }
    }

    /// <summary>
    /// Создает новый Listener с уникальным тегом, добавляет в словарь но не отправляет запрос.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    internal MikroTikResponseListener AddListener()
    {
        CheckDisposed();

        var tag = CreateUniqueTag(); // Создать уникальный tag.
        var listener = new MikroTikResponseListener(tag, this);
        _listeners.Add(tag, listener); // Добавить в словарь.
        return listener;
    }

    /// <summary>
    /// Потокобезопасно создает уникальный тег.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    internal string CreateUniqueTag()
    {
        CheckDisposed();

        return _connection.CreateUniqueTag();
    }

    /// <summary>
    /// Потокобезопасно добавляет в очередь задание — получить фрейм с указанным тегом.
    /// </summary>
    /// <param name="tagToReceive">Тег сообщения которое нужно прочитать из сокета.</param>
    /// <exception cref="IOException">Обрыв соединения.</exception>
    /// <exception cref="ObjectDisposedException"/>
    internal bool AddTagToRead(string tagToReceive)
    {
        CheckDisposed();

        lock (_framesToRead)
        {
            // Если был обрыв соединения то продолжать нельзя.
            if (_socketException == null)
            {
                // Если в словаре уже есть такой тег то увеличить ему счетчик.
                ref var countRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_framesToRead, tagToReceive, out var exists);
                countRef++; // Добавить работы потоку который сейчас читает из сокета.

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

    /// <exception cref="ObjectDisposedException"/>
    internal MikroTikResponseFrameDictionary Listen(MikroTikResponseListener listener, int millisecondsTimeout, CancellationToken cancellationToken)
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

    /// <exception cref="ObjectDisposedException"/>
    internal ValueTask<MikroTikResponseFrameDictionary> ListenAsync(MikroTikResponseListener listener, int millisecondsTimeout, CancellationToken cancellationToken)
    {
        CheckDisposed();

        // Взять из кэша или ждать поступления.
        if (TryTakeBeforeListen(listener, out var frames))
        {
            return ValueTask.FromResult(frames);
        }
        else
        {
            return listener.TakeAsync(millisecondsTimeout, cancellationToken);
        }
    }

    /// <exception cref="ObjectDisposedException"/>
    internal void StartBackgroundRead()
    {
        CheckDisposed();
        _backgroundReceive = ReadAllFrames(); // Читаем из сокета.
    }

    internal Task CancelListenersAsync(bool waitACK, CancellationToken cancellationToken)
    {
        CheckDisposed();

        if (waitACK)
        {
            // Отправляем команду и ждем ответ.
            return ExecuteAsyncCore(new MikroTikCommand("/cancel"), cancellationToken);
        }
        else
        {
            // Что бы не происходило ожидание завершения команды 
            // нужно сделать её тегированной и не добавлять в словарь.

            var selfTag = CreateUniqueTag();
            var cancelAllcommand = new MikroTikCancelAllCommand(selfTag);

            // Отправка команды без ожидания ответа.
            return SendAsync(cancelAllcommand, cancellationToken);
        }
    }

    internal void CancelListeners(bool waitACK, CancellationToken cancellationToken)
    {
        CheckDisposed();

        if (waitACK)
        {
            ExecuteCore(new MikroTikCommand("/cancel"), cancellationToken); // Отправляем команду и ждем ответ.
        }
        else
        {
            var selfTag = CreateUniqueTag();
            var cancelAllcommand = new MikroTikCancelAllCommand(selfTag);
            Send(cancelAllcommand, cancellationToken); // Отправка команды без ожидания ответа.
        }
    }

    /// <summary>
    /// Синхронная отправка запроса в сокет без получения ответа. Не проверяет переиспользование команды.
    /// </summary>
    internal void Send(MikroTikCommand command, CancellationToken cancellationToken)
    {
        CheckDisposed();

        _sender.Invoke(static (mt, s) => mt.SendCommandInLooper(s), command, cancellationToken);
    }

    /// <summary>
    /// Асинхронная отправка запроса в сокет без получения ответа. Не проверяет переиспользование команды.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    internal Task SendAsync(MikroTikCommand command, CancellationToken cancellationToken)
    {
        CheckDisposed();

        return _sender.InvokeAsync(static (con, s) =>
        {
            return con.SendCommandInLooperAsync(s);
        }, command, cancellationToken);
    }

    /// <summary>
    /// Синхронная отправка запроса и получение ответа.
    /// </summary>
    internal MikroTikResponse ExecuteCore(MikroTikCommand command, CancellationToken cancellationToken)
    {
        CheckDisposed();

        var readerIsStopped = AddTagToRead("");

        // Отправка команды в сокет через очередь.
        _sender.Invoke(static (mt, s) => mt.SendCommandInLooper(s), command, cancellationToken);

        if (readerIsStopped)
        {
            // Запустить чтение из сокета.
            StartBackgroundRead();
        }

        // Ожидает не тегированный ответ из сокета.
        return _mainQueue.Take(Timeout.Infinite, cancellationToken);
    }

    /// <summary>
    /// Асинхронная отправка запроса в сокет и получение результата.
    /// </summary>
    internal Task<MikroTikResponse> ExecuteAsync(MikroTikCommand command, CancellationToken cancellationToken)
    {
        CheckDisposed();

        command.CheckAndMarkAsUsed();
        return ExecuteAsyncCore(command, cancellationToken); // Отправка и получение ответа через очередь.
    }

    /// <summary>
    /// Синхронная отправка запроса в сокет и получение результата.
    /// </summary> 
    internal MikroTikResponse Execute(MikroTikCommand command, CancellationToken cancellationToken)
    {
        CheckDisposed();

        command.CheckAndMarkAsUsed();
        var response = ExecuteCore(command, cancellationToken); // Отправка и получение ответа через очередь.
        return response;
    }

    /// <summary>
    /// Отправляет в сокет запрос на отмену не дожидаясь результата.
    /// </summary>
    /// <param name="tag">Тег связанной операции которую следует отменить.</param>
    internal Task CancelListenerNoWaitAsync(string tag, CancellationToken cancellationToken)
    {
        CheckDisposed();

        // Подписываемся на завершение отмены
        var cancelCommand = CreateCancelCommand(tag);

        // Отправить из другого потока.
        return _sender.InvokeAsync(static (mt, s) => mt.SendCommandInLooperAsync(s), cancelCommand, cancellationToken);
    }

    /// <summary>
    /// Создает команду для отмены и добавляет в словарь.
    /// </summary>
    internal MikroTikCancelCommand CreateCancelCommand(string tag)
    {
        CheckDisposed();

        var selfTag = CreateUniqueTag();
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
    internal bool Quit(int millisecondsTimeout, CancellationToken cancellationToken)
    {
        CheckDisposed();

        // Подписываемся на завершение отмены.
        var quitCommand = new MikroTikQuitCommand();

        // Добавляем в словарь.
        _listeners.AddQuit(quitCommand);

        // Добавить задание.
        var readerIsStopped = AddTagToRead("");

        // Обязательно захватить блокировку перед отправкой.
        lock (quitCommand)
        {
            // Отправляем команду без ожидания ответа.
            Send(quitCommand, cancellationToken);

            if (readerIsStopped)
            {
                // Запустить чтение из сокета.
                StartBackgroundRead();
            }

            // Ждем получение !trap.
            return quitCommand.Wait(millisecondsTimeout);
        }
    }

    /// <summary>
    /// Сообщает серверу что выполняется разъединение.
    /// </summary>
    /// <param name="millisecondsTimeout">Позволяет подождать подтверждение от сервера что-бы лишний раз не происходило исключение в потоке читающем из сокета.</param>
    internal async Task<bool> QuitAsync(int millisecondsTimeout, CancellationToken cancellationToken)
    {
        CheckDisposed();

        var quitCommand = new MikroTikAsyncQuitCommand();

        // Добавляем в словарь.
        _listeners.AddQuit(quitCommand);

        var readerIsStopped = AddTagToRead("");

        // Отправляем команду без ожидания ответа.
        await SendAsync(quitCommand, cancellationToken).ConfigureAwait(false);

        if (readerIsStopped)
        {
            // Запустить чтение из сокета.
            StartBackgroundRead();
        }

        // Ждем получение !trap.
        return await quitCommand.WaitAsync(millisecondsTimeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Создает команду для отмены и добавляет в словарь.
    /// </summary>
    internal MikroTikAsyncCancelCommand CreateAsyncCancelCommand(string tag)
    {
        CheckDisposed();

        var selfTag = CreateUniqueTag();
        var command = new MikroTikAsyncCancelCommand(tag, selfTag, this);

        // Подписываемся на завершение отмены.
        _listeners.Add(command.SelfTag, command);

        return command;
    }

    /// <summary>
    /// Удаляет подписчика из словаря. Потокобезопасно.
    /// </summary>
    internal void RemoveListener(string tag)
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
            StartBackgroundRead();
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
    private async Task ReadAllFrames()
    {
        Debug.Assert(_backgroundReceive.IsCompleted);

        try
        {
        ReadNextFrame:
            var receivedTrap = false; // если получен !trap
            var receivedDone = false; // если получен !done
            var receivedFatal = false; // если получен !fatal
            string? receivedTagId = null; // если получен .tag=
            string? fatalMessage = null;
            var fullResponse = new MikroTikResponse();
            var frames = new MikroTikResponseFrameDictionary();
            int count;

            _receiveTimeout.StartWatchdog();
            try
            {
                while (true) // Читаем фреймы пока они не закончатся.
                {
                    await _stream.ReadBlockAsync(_readBuffer.Slice(0, 1)).ConfigureAwait(false); // Чтение заголовка строки.

                    if (_readBuffer.Span[0] == 0) // Конец сообщения.
                    {
                        if (receivedTagId != null) // Получен фрейм сообщения маркированный тегом.
                        {
                            if (_listeners.TryGetValue(receivedTagId, out var listener))
                            {
                                lock (listener.SyncObj)
                                {
                                    if (receivedDone)
                                    {
                                        listener.Done(); // Работа с этим подписчиком завершена.
                                    }
                                    else
                                    {
                                        if (!receivedTrap)
                                        {
                                            listener.AddResult(frames);
                                        }
                                        else
                                        {
                                            listener.AddTrap(new MikroApiTrapException(frames));
                                        }
                                    }
                                }
                            }

                            if (TryExit(receivedTagId))
                            {
                                return; // Завершение потока.
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
                            if (receivedFatal) // Сервер закрывает соединение.
                            {
                                ConnectionClosed(new MikroApiFatalException(fatalMessage!), gotFatal: true);
                                return; // Завершение потока. Текущий экземпляр MikroTikSocket больше использовать нельзя.
                            }

                            if (frames.Count > 0)
                            {
                                fullResponse.Add(frames);
                            }

                            if (receivedTrap)
                            {
                                var trapException = new MikroApiTrapException(fullResponse[0]);
                                _mainQueue.AddTrap(trapException);
                            }

                            if (receivedDone) // Сообщение получено полностью.
                            {
                                _mainQueue.AddResult(fullResponse); // Положить в основную очередь.

                                if (TryExit(""))
                                {
                                    return; // Завершение потока.
                                }
                                else
                                {
                                    goto ReadNextFrame;
                                }
                            }
                        }

                        frames = new MikroTikResponseFrameDictionary(); // Нельзя делать Clear потому что fullResponse.Add(frame).
                        continue; // Возвращаемся к чтению заголовка.
                    }
                    else
                    {
                        // Длина строки в байтах.
                        count = await GetSizeAsync(_readBuffer).ConfigureAwait(false);
                    }

                    var buf = _readBuffer;
                    if (count > _readBuffer.Length)
                    {
                        buf = new byte[count];
                    }

                    // Чтение строки.
                    await _stream.ReadBlockAsync(buf.Slice(0, count)).ConfigureAwait(false);
                    var word = _encoding.GetString(buf.Slice(0, count).Span);

                    Debug.WriteLine(word);

                    if (word == "!re")
                    {
                    }
                    else if (!receivedDone && word == "!done")
                    {
                        receivedDone = true;
                    }
                    else if (!receivedFatal && word == "!fatal")
                    {
                        receivedFatal = true;
                    }
                    else if (!receivedTrap && word == "!trap")
                    {
                        receivedTrap = true;
                    }
                    else
                    {
                        var pos = word.IndexOf('=', 1);
                        if (pos >= 0)
                        {
                            var key = word.Substring(1, pos - 1);
                            var value = word.Substring(pos + 1);

                            if (key == "tag") // Этот ответ тегирован.
                            {
                                receivedTagId = value;
                            }
                            else
                            {
                                frames.TryAdd(key, value);
                            }
                        }
                        else if (receivedFatal)
                        {
                            fatalMessage = word;
                        }
                    }
                }
            }
            finally
            {
                _receiveTimeout.StopWatchdog(); // Может бросить исключение.
            }
        }
        catch (Exception rcvError) // Произошел обрыв сокета.
        {
            ConnectionClosed(rcvError, gotFatal: false);
            return; // Завершение потока. Текущий экземпляр MikroTikSocket больше использовать нельзя.
        }

        #region TryExit
        
        bool TryExit(string receivedTag) // Считано сообщение или фрейм.
        {
            lock (_framesToRead)
            {
                ref var tagCountRef = ref CollectionsMarshal.GetValueRefOrNullRef(_framesToRead, receivedTag);
                if (!Unsafe.IsNullRef(ref tagCountRef))
                {
                    if (tagCountRef == 1)
                    {
                        _framesToRead.Remove(receivedTag); // Прочитаны все фреймы с таким тегом.
                        if (_framesToRead.Count == 0) // Все сообщения были прочитаны.
                        {
                            _reading = false; // Завершаем поток чтения.
                            return true;
                        }
                    }
                    else
                    {
                        tagCountRef--; // Требуется продолжить читать фреймы для этого тега.
                    }
                }
                
                return false; // Продолжить чтение сообщений.
            }
        }

        #endregion
    }

    /// <summary>
    /// Асинхронно отправляет запрос и получает ответ через основную очередь.
    /// </summary>
    private async Task<MikroTikResponse> ExecuteAsyncCore(MikroTikCommand command, CancellationToken cancellationToken)
    {
        Debug.Assert(_sender != null);

        var readerIsStopped = AddTagToRead("");

        // Отправка команды в сокет.
        await _sender.InvokeAsync(static (mt, s) => mt.SendCommandInLooperAsync(s), command, cancellationToken).ConfigureAwait(false);

        if (readerIsStopped)
        {
            // Запустить чтение из сокета.
            StartBackgroundRead();
        }

        // Ожидает не тегированный ответ из сокета.
        return await _mainQueue.TakeAsync(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
    }

    private Memory<byte> Encode(MikroTikCommand command)
    {
        var offset = 0;

        // Учитываем Null-терминатор.
        var totalLength = 1;

        var useSendBuffer = true;

        foreach (var line in CollectionsMarshal.AsSpan(command._lines))
        {
            var lineBytesCount = _encoding.GetByteCount(line); // Длина строки.
            totalLength += lineBytesCount;
            var encodedLength = EncodeLengthCount((uint)lineBytesCount); // Размер длины строки.
            totalLength += encodedLength;

            // Если влезет в буффер.
            if (totalLength <= _sendBuffer.Length)
            {
                // Записать в буффер длину строки.
                GetPayloadLengthBytesCount((uint)lineBytesCount, _sendBuffer.Slice(offset).Span);

                offset += encodedLength;

                // Записать в буффер строку.
                _encoding.GetBytes(line, _sendBuffer.Slice(offset).Span);

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
            var buffer = new byte[totalLength];
            foreach (var line in CollectionsMarshal.AsSpan(command._lines))
            {
                var lineBytesCount = _encoding.GetByteCount(line);
                offset += GetPayloadLengthBytesCount((uint)lineBytesCount, buffer);
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

    /// <param name="len">Размер блока в байтах.</param>
    /// <returns>Число байт записанных в <paramref name="dest"/>.</returns>
    private static int GetPayloadLengthBytesCount(uint len, Span<byte> dest)
    {
        if (len < 128)
        {
            dest[0] = (byte)len;
            return 1;
        }
        if (len < 16384)
        {
            var value = (len | 0x8000);
            dest[0] = (byte)(value >> 8);
            dest[1] = (byte)(value >> 16);
            return 2;
        }
        if (len < 0x200000)
        {
            var value = (len | 0xC00000);
            dest[0] = (byte)(value >> 16);
            dest[1] = (byte)(value >> 8);
            dest[2] = (byte)(value);
            return 3;
        }
        if (len < 0x10000000)
        {
            var value = (len | 0xE0000000);
            dest[0] = (byte)(value >> 24);
            dest[1] = (byte)(value >> 16);
            dest[2] = (byte)(value >> 8);
            dest[3] = (byte)(value);
            return 4;
        }
        else
        {
            dest[0] = 0xF0;
            dest[1] = (byte)(len >> 24);
            dest[2] = (byte)(len >> 16);
            dest[3] = (byte)(len >> 8);
            dest[4] = (byte)(len);
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
        var buffer = Encode(command);
        try
        {
            var timeout = _sendTimeout.StartWatchdog();
            try
            {
                _stream.Write(buffer.Span);
            }
            finally
            {
                timeout.StopTimer();
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
    private async Task SendCommandInLooperAsync(MikroTikCommand command)
    {
        Debug.WriteLine(Environment.NewLine + command + Environment.NewLine);
        var buffer = Encode(command);

        try
        {
            var timeout = _sendTimeout.StartWatchdog();
            try
            {
                await _stream.WriteAsync(buffer).ConfigureAwait(false);
            }
            finally
            {
                timeout.StopTimer();
            }
        }
        catch (Exception ex)
        {
            OnSendExceptionInLooper(ex);
        }
    }

    private void OnSendExceptionInLooper(Exception sendError)
    {
        lock (_framesToRead)
        {
            // Поток читающий из сокета уже мог установить исключение.
            if (_socketException == null)
            {
                _socketException = sendError;
                _framesToRead.Clear();
            }
            else
            {
                // Взять более ранее исключение.
                sendError = _socketException;
            }
        }

        // Удалить всех подписчиков из словаря и запретить дальнейшее добавление.
        _listeners.AddCriticalException(sendError, gotFatal: false);

        // Сообщить основному подписчику что произошел обрыв сокета.
        _mainQueue.AddCriticalException(sendError);

        throw sendError;
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

            var v = 0;
            for (var i = 0; i < 2; i++)
            {
                v = (v << 8) + buffer.Span[i];
            }

            return v ^ 0x8000;
        }
        else if (buffer.Span[0] < 224)
        {
            await _stream.ReadBlockAsync(buffer.Slice(1, 2)).ConfigureAwait(false);

            var v = 0;
            for (var i = 0; i < 3; i++)
            {
                v = (v << 8) + buffer.Span[i];
            }

            return v ^ 0xC00000;
        }
        else if (buffer.Span[0] < 240)
        {
            await _stream.ReadBlockAsync(buffer.Slice(1, 3)).ConfigureAwait(false);

            var v = 0;
            for (var i = 0; i < 4; i++)
            {
                v = (v << 8) + buffer.Span[i];
            }

            return (int)(v ^ 0xE0000000);
        }
        else if (buffer.Span[0] == 240)
        {
            await _stream.ReadBlockAsync(buffer.Slice(0, 4)).ConfigureAwait(false);

            var v = 0;
            for (var i = 0; i < 4; i++)
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
        lock (_connectionSync)
        {
            // Этот stream мог уже быть уничтожен ватчдогом.
            if (!_socketDisposed)
            {
                _socketDisposed = true;
                _stream.Dispose();
                _tcpClient.Dispose();
            }
        }
    }

    /// <exception cref="ObjectDisposedException"/>
    [MemberNotNull(nameof(_sender))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        if (!_disposed)
        {
            Debug.Assert(_sender != null);
            return;
        }

        ThrowHelper.ThrowConnectionDisposed();
    }
}
