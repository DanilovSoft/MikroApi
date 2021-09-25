using System;
using System.Data;
using System.Diagnostics;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DanilovSoft.MikroApi.Helpers;

namespace DanilovSoft.MikroApi
{
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

        public void Send<TState>(Action<MtOpenConnection, TState> callback, TState state)
        {
            lock (_sendObj)
            {
                CheckNotFinished();
                WaitForeignTask(_sendTask);
                callback(_connection, state);
            }
        }

        public Task SendAsync<TState>(Func<MtOpenConnection, TState, Task> callback, TState state)
        {
            lock (_sendObj)
            {
                CheckNotFinished();
                _sendTask = WaitForeignTaskAndSend(callback, state);
                return _sendTask;
            }
        }

        public void Quit()
        {
            lock (_sendObj)
            {
                WaitForeignTask(_sendTask);
                _finished = true;
            }
        }

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
        private static void WaitForeignTask(Task task)
        {
            if (!task.IsCompleted)
            {
                task.ContinueWith(_ => { },
                            CancellationToken.None,
                            TaskContinuationOptions.None,
                            TaskScheduler.Default)
                            .GetAwaiter()
                            .GetResult();
            }
        }

        /// <summary>
        /// Ожидает чужой таск не провоцируя исключения.
        /// </summary>
        private static Task WaitForeignTaskAsync(Task task)
        {
            if (!task.IsCompleted)
            {
                return task.ContinueWith(_ => { },
                            CancellationToken.None,
                            TaskContinuationOptions.None,
                            TaskScheduler.Default);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private Task WaitForeignTaskAndSend<TState>(Func<MtOpenConnection, TState, Task> callback, TState state)
        {
            Debug.Assert(Monitor.IsEntered(_sendObj));

            if (_sendTask.IsCompleted)
            {
                return callback(_connection, state);
            }
            else
            {
                return Wait(_sendTask, callback, state, _connection);
            }

            static async Task Wait(Task sendTask, Func<MtOpenConnection, TState, Task> callback, TState state, MtOpenConnection connection)
            {
                await WaitForeignTaskAsync(sendTask).ConfigureAwait(false);
                await callback(connection, state).ConfigureAwait(false);
            }
        }
    }
}
