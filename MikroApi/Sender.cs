using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MikroApi.Helpers;

namespace DanilovSoft.MikroApi;

internal sealed class Sender
{
    private readonly object _sendObj = new();
    private readonly MtOpenConnection _connection;
    private Task _sendTask = Task.CompletedTask;
    private bool _finished;

    public Sender(MtOpenConnection mikroTikSocket)
    {
        _connection = mikroTikSocket;
    }

    /// <exception cref="OperationCanceledException"/>
    public void Invoke(Action<MtOpenConnection, MikroTikCommand> callback, MikroTikCommand state, CancellationToken cancellationToken)
    {
        lock (_sendObj)
        {
            CheckNotFinished();
            WaitSendTask(cancellationToken);
            callback(_connection, state);
        }
    }

    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="ObjectDisposedException"/>
    public Task InvokeAsync(Func<MtOpenConnection, MikroTikCommand, Task> callback, MikroTikCommand state, CancellationToken cancellationToken)
    {
        lock (_sendObj)
        {
            CheckNotFinished();
            _sendTask = WaitTaskAndSend(callback, state, cancellationToken);
            return _sendTask;
        }
    }

    public void Quit(CancellationToken cancellationToken = default)
    {
        lock (_sendObj)
        {
            WaitSendTask(cancellationToken);
            _finished = true;
        }
    }

    /// <exception cref="ObjectDisposedException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckNotFinished()
    {
        Debug.Assert(Monitor.IsEntered(_sendObj));

        if (!_finished)
        {
            return;
        }

        ThrowHelper.ThrowConnectionDisposed();
    }

    /// <summary>
    /// Ожидает чужой таск не провоцируя исключения.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    private void WaitSendTask(CancellationToken cancellationToken)
    {
        var task = _sendTask;

        if (!task.IsCompleted)
        {
            task.ContinueWith(_ => { },
                        cancellationToken,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default)
                        .GetAwaiter()
                        .GetResult();
        }
    }

    /// <summary>
    /// Ожидает чужой таск не провоцируя исключения.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    private Task WaitForeignTaskAsync(CancellationToken cancellationToken)
    {
        var task = _sendTask;

        if (!task.IsCompleted)
        {
            return task.ContinueWith(_ => { },
                        cancellationToken,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    private Task WaitTaskAndSend(Func<MtOpenConnection, MikroTikCommand, Task> callback, MikroTikCommand state, CancellationToken cancellationToken)
    {
        Debug.Assert(Monitor.IsEntered(_sendObj));

        if (_sendTask.IsCompleted)
        {
            return callback(_connection, state);
        }
        else
        {
            return WaitTaskAndSendAsync(callback, state, _connection, cancellationToken);
        }
    }

    private async Task WaitTaskAndSendAsync(Func<MtOpenConnection, MikroTikCommand, Task> callback, MikroTikCommand state, MtOpenConnection connection, CancellationToken ct)
    {
        await WaitForeignTaskAsync(ct).ConfigureAwait(false);
        await callback(connection, state).ConfigureAwait(false);
    }
}
