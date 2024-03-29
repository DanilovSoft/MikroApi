﻿using DanilovSoft.MikroApi.Threading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Принимает сообщения инициализированные командой /listen.
    /// </summary>
    [DebuggerDisplay(@"\{{_queue}\}")]
    public sealed class MikroTikResponseListener : IMikroTikResponseListener
    {
        private readonly ListenerQueue<MikroTikResponseFrameDictionary> _queue = new();
        private readonly MtOpenConnection _connection;
        /// <summary>
        /// Тег связанный с текущим подписчиком.
        /// </summary>
        internal readonly string _tag;
        /// <summary>
        /// <see langword="true"/> если в текущем экземпляре уже выполнялась отмена.
        /// </summary>
        public bool IsCanceled { get; private set; }
        /// <summary>
        /// <see langword="true"/> если связанная операция была завершена.
        /// </summary>
        public bool IsComplete => _queue.IsDone;
        internal object SyncObj => _queue;
        object IMikroTikResponseListener.SyncObj => _queue;
        //private int _disposed;

        /// <summary>
        /// Принимает сообщения инициализированные командой /listen.
        /// </summary>
        /// <param name="tag">Тег связанный с текущим подписчиком.</param>
        /// <param name="socket"></param>
        internal MikroTikResponseListener(string tag, MtOpenConnection socket)
        {
            _connection = socket;
            _tag = tag;
        }

        /// <summary>
        /// Отправляет запрос на отмену и дожидается подтверждения об упешной отмене.
        /// </summary>
        public void Cancel()
        {
            Cancel(wait: true);
        }

        /// <summary>
        /// Отправляет запрос на отмену и дожидается подтверждения об упешной отмене.
        /// </summary>
        /// <param name="wait">True если нужно дождаться подтверждения об успешной отмене. Значение по умолчанию True</param>
        public void Cancel(bool wait)
        {
            ThrowIfCanceled();

            // В любом случае нужен собственный тег что-бы результат не попал в основную очередь когда wait = false
            string selfTag = _connection.CreateUniqueTag();
            var cancelCommand = new MikroTikCancelCommand(_tag, selfTag, _connection);

            if (wait)
            {
                // Добавляем команду в словарь (подписываемся на завершение отмены).
                _connection._listeners.Add(selfTag, cancelCommand);

                // Добавляем сокету тег в очередь ожидания.
                bool readerIsStopped = _connection.AddTagToRead(selfTag);

                // Важно захватить блокировку перед отправкой в сокет.
                lock (cancelCommand)
                {
                    // Отправить запрос без ожидания ответа.
                    _connection.Send(cancelCommand);

                    if (readerIsStopped)
                    {
                        // Запуск потока для чтения.
                        _connection.StartRead();
                    }

                    // Этот Listener больше нельзя отменять.
                    SetCanceled();

                    // Ожидаем тегированное подтверждение от сервера.
                    cancelCommand.Wait();
                }
            }
            else
            {
                // Отправить запрос без ожидания ответа.
                _connection.Send(cancelCommand);

                // Этот Listener больше нельзя отменять.
                SetCanceled();
            }
        }

        /// <summary>
        /// Отправляет запрос на отмену и дожидается подтверждения об упешной отмене.
        /// </summary>
        public Task CancelAsync()
        {
            return CancelAsync(wait: true);
        }

        /// <summary>
        /// Отправляет запрос на отмену.
        /// </summary>
        /// <param name="wait">True если нужно дождаться подтверждения об успешной отмене. Значение по умолчанию True</param>
        public async Task CancelAsync(bool wait)
        {
            ThrowIfCanceled();

            if (wait)
            {
                // Подписываемся на завершение отмены.
                MikroTikAsyncCancelCommand cancelCommand = _connection.CreateAsyncCancelCommand(_tag);

                // Добавляем задание для потока читающего из сокета.
                bool readerIsStopped = _connection.AddTagToRead(cancelCommand.SelfTag);

                // Отправляем тегированный запрос на отмену без ожидания результата.
                await _connection.SendAsync(cancelCommand).ConfigureAwait(false);

                if (readerIsStopped)
                {
                    // Запуск потока для чтения.
                    _connection.StartRead();
                }

                // Этот Listener больше нельзя отменять.
                SetCanceled();

                // Ожидаем тегированное подтверждение от сервера.
                await cancelCommand.WaitDoneAsync().ConfigureAwait(false);
            }
            else
            {
                // Отправляем тегированный запрос на отмену без ожидания результата.
                await _connection.CancelListenerNoWaitAsync(_tag).ConfigureAwait(false);

                // Этот Listener больше нельзя отменять
                SetCanceled();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cachedResult"></param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <returns></returns>
        internal bool TryTake([NotNullWhen(true)] out MikroTikResponseFrameDictionary? cachedResult) => _queue.TryTake(out cachedResult);

        internal MikroTikResponseFrameDictionary Take(int millisecondsTimeout, CancellationToken cancellationToken) =>
            _queue.Take(millisecondsTimeout, cancellationToken);

        internal ValueTask<MikroTikResponseFrameDictionary> TakeAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return _queue.TakeAsync(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Устанавливает флаг <see cref="IsCanceled"/>.
        /// После этого текущий экземпляр больше отменять нельзя.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetCanceled()
        {
            IsCanceled = true;
        }

        /// <summary>
        /// Генерирует исключение если текущий Listener уже был отменён.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfCanceled()
        {
            if (IsCanceled)
            {
                throw new InvalidOperationException("This listener is already canceled");
            }
        }

        #region Реализация интерфейса

        /// <summary>
        /// Добавляет результат в коллекцию.
        /// Вызывается потоком читающим из сокета.
        /// </summary>
        void IMikroTikResponseListener.AddResult(MikroTikResponseFrameDictionary result)
        {
            _queue.AddResult(result);
        }

        // Получен "!trap" потоком читающим из сокета.
        // Чаще всего это подтверждение отмены и за этим !trap последует !done.
        /// <summary>
        /// Добавляет исключение как результат в коллекцию.
        /// </summary>
        void IMikroTikResponseListener.AddTrap(MikroApiTrapException trapException)
        {
            _queue.AddTrap(trapException);
        }

        /// <summary>
        /// Добавляет исключение как результат в коллекцию.
        /// Вызывается потоком читающим из сокета.
        /// </summary>
        void IMikroTikResponseListener.AddCriticalException(Exception exception)
        {
            _queue.AddCriticalException(exception);
        }

        /// <summary>
        /// Добавляет исключение как результат в коллекцию.
        /// Вызывается потоком читающим из сокета.
        /// </summary>
        void IMikroTikResponseListener.AddFatal(Exception exception)
        {
            _queue.AddFatal(exception);
        }

        /// <summary>
        /// Получен "!done" — сервер прекратил отправку сообщений для этого подписчика. Подписчик удаляется из словаря.
        /// </summary>
        void IMikroTikResponseListener.Done()
        {
            // Установить коллекцию в завершенное состояние.
            _queue.Done();

            // Удалить текущий listener из словаря.
            _connection.RemoveListener(_tag);
        }

        #endregion

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public MikroTikResponseFrameDictionary ListenNext() => ListenNext(millisecondsTimeout: -1, CancellationToken.None);

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public MikroTikResponseFrameDictionary ListenNext(int millisecondsTimeout) => ListenNext(millisecondsTimeout, CancellationToken.None);

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public MikroTikResponseFrameDictionary ListenNext(CancellationToken cancellationToken) => ListenNext(millisecondsTimeout: -1, cancellationToken);

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public MikroTikResponseFrameDictionary ListenNext(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // Запускает поток для чтения, если это необходимо или забирает результат из кэша.
            return _connection.Listen(this, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="MikroApiTrapException"/>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public ValueTask<MikroTikResponseFrameDictionary> ListenNextAsync()
        {
            return ListenNextAsync(millisecondsTimeout: -1, CancellationToken.None);
        }

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="MikroApiTrapException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public ValueTask<MikroTikResponseFrameDictionary> ListenNextAsync(int millisecondsTimeout)
        {
            return ListenNextAsync(millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="MikroApiTrapException"/>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public ValueTask<MikroTikResponseFrameDictionary> ListenNextAsync(CancellationToken cancellationToken)
        {
            return ListenNextAsync(millisecondsTimeout: -1, cancellationToken);
        }

        /// <summary>
        /// Ожидает очередной ответ от сервера.
        /// </summary>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="MikroApiTrapException"/>
        /// <exception cref="MikroApiDoneException"/>
        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="MikroApiCommandInterruptedException"/>
        public ValueTask<MikroTikResponseFrameDictionary> ListenNextAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // Запускает поток для чтения, если это необходимо или забирает результат из кэша.
            return _connection.ListenAsync(this, millisecondsTimeout, cancellationToken);
        }
    }
}
