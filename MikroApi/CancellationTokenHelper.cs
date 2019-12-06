using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ExceptionMessages;

namespace DanilovSoft.MikroApi
{
    internal class CancellationTokenHelper
    {
        private readonly object _sync = new object();
        private readonly IDisposable _disposable;
        private readonly CancellationToken _cancellationToken;
        private readonly TimeSpan _timeout;
        private volatile bool _isDisposed;
        public bool IsDisposed => _isDisposed;
        private int _atomicDisposed = 0;

        public CancellationTokenHelper(IDisposable disposable, TimeSpan timeout, CancellationToken cancellationToken)
        {
            _disposable = disposable;
            _timeout = timeout;
            _cancellationToken = cancellationToken;
        }

        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="TimeoutException"/>
        public Task WrapAsync(Func<Task> asyncAction)
        {
            return WrapAsync(asyncAction());
        }

        /// <exception cref="OperationCanceledException"/>
        /// <exception cref="TimeoutException"/>
        public async Task WrapAsync(Task task)
        {
            int atomicDispose;

            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken))
            {
                linked.CancelAfter(_timeout);

                using (linked.Token.Register(DisposeCallback, false))
                {
                    try
                    {
                        // Исключение может произойти из-за преждевременного закрытия сокета.
                        await task.ConfigureAwait(false);
                    }
                    catch (Exception ex) when (_cancellationToken.IsCancellationRequested)
                    /* Пользователь отменил операцию */
                    {
                        throw new OperationCanceledException(OperationCanceledMessage, ex, _cancellationToken);
                    }
                    catch (Exception ex) when (linked.IsCancellationRequested)
                    /* Превышен таймаут */
                    {
                        throw new TimeoutException(ConnectTimeoutExceptionMessage, ex);
                    }
                    finally
                    {
                        /* Отменяем Dispose */
                        atomicDispose = Interlocked.CompareExchange(ref _atomicDisposed, 2, 0);
                    }
                }
            }

            // в редких случаях мы можем не успеть отписаться от DisposeCallback,
            // поэтому дополнительно проверяем атомарный флаг
            if (atomicDispose == 1)
            {
                /* Блокировка позволяет дождаться завершения _disposable.Dispose() */
                lock (_sync)
                {
                    if(_cancellationToken.IsCancellationRequested) /* Пользователь отменил операцию */
                        throw new OperationCanceledException(OperationCanceledMessage, _cancellationToken);

                    /* Превышен таймаут */
                    throw new TimeoutException(ConnectTimeoutExceptionMessage);
                }
            }
        }

        private void DisposeCallback()
        {
            lock (_sync)
            {
                if (Interlocked.CompareExchange(ref _atomicDisposed, 1, 0) == 0)
                {
                    try
                    {
                        _disposable.Dispose();
                    }
                    catch { }

                    _isDisposed = true;
                }
            }
        }
    }
}
