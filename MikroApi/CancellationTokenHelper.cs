using System;
using System.Threading;
using System.Threading.Tasks;
using static ExceptionMessages;

namespace DanilovSoft.MikroApi;

internal sealed class CancellationTokenHelper
{
    private readonly object _sync = new();
    private readonly IDisposable _disposable;
    private readonly CancellationToken _cancellationToken;
    private readonly TimeSpan _timeout;
    private volatile bool _isDisposed;
    private int _atomicDisposed;

    public CancellationTokenHelper(IDisposable disposable, TimeSpan timeout, CancellationToken cancellationToken)
    {
        _disposable = disposable;
        _timeout = timeout;
        _cancellationToken = cancellationToken;
    }

    public bool IsDisposed => _isDisposed;

    /// <exception cref="TimeoutException"/>
    /// <exception cref="OperationCanceledException"/>
    public Task WrapAsync(Func<ValueTask> asyncAction)
    {
        return WrapAsync(asyncAction());
    }

    /// <exception cref="TimeoutException"/>
    /// <exception cref="OperationCanceledException"/>
    public async Task WrapAsync(ValueTask task)
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
                // Пользователь отменил операцию.
                {
                    throw new OperationCanceledException(OperationCanceledMessage, ex, _cancellationToken);
                }
                catch (Exception ex) when (linked.IsCancellationRequested)
                // Превышен таймаут.
                {
                    throw new TimeoutException(ConnectTimeoutExceptionMessage, ex);
                }
                finally
                {
                    // Отменяем Dispose.
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
                if (_cancellationToken.IsCancellationRequested) /* Пользователь отменил операцию */
                {
                    throw new OperationCanceledException(OperationCanceledMessage, _cancellationToken);
                }

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
