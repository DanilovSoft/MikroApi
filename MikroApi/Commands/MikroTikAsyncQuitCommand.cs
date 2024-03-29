﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    /*
    >>> /quit
    <<< !fatal
    <<< session terminated on request
     */
    internal class MikroTikAsyncQuitCommand : MikroTikCommand, IMikroTikResponseListener
    {
        private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        // Это свойство требуется интерфейсом но не участвует в синхронизации потоков.
        private readonly object _syncObj = new();
        object IMikroTikResponseListener.SyncObj => _syncObj;
        private volatile Exception? _criticalException;

        // ctor
        internal MikroTikAsyncQuitCommand() : base("/quit")
        {

        }

        /// <summary>
        /// Возвращает <see langword="true"/> если был получен !fatal.
        /// Не бросает исключения. Вместо этого возвращает <see langword="false"/>.
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        internal async Task<bool> WaitAsync(int millisecondsTimeout)
        {
            using (var cts = new CancellationTokenSource(millisecondsTimeout))
            {
                bool success;
                using (cts.Token.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(false), _tcs, false))
                {
                    success = await _tcs.Task.ConfigureAwait(false);
                }

                if (success)
                {
                    var ex = _criticalException;
                    if (ex != null)
                    {
                        //Debug.WriteLine(string.Format(MikroTikQuitCommand.ExceptionDebugMessage, ex.Message));
                        return false;
                    }
                }
                return success;
            }
        }

        // !fatal получим здесь.
        void IMikroTikResponseListener.AddFatal(Exception exception)
        {
            _tcs.TrySetResult(true);
        }

        void IMikroTikResponseListener.AddCriticalException(Exception exception)
        {
            _criticalException = exception;
            _tcs.TrySetResult(true);
        }

        #region Не используемые члены интерфейса
        // Не может произойти.
        void IMikroTikResponseListener.AddResult(MikroTikResponseFrameDictionary message) { }
        // Не может произойти.
        void IMikroTikResponseListener.AddTrap(MikroApiTrapException trapException) { }
        // Не может произойти.
        void IMikroTikResponseListener.Done() { }
        #endregion
    }
}
