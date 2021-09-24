using System;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Команда отмены поддерживающая асинхронное ожидание подтверждения.
    /// </summary>
    internal class MikroTikAsyncCancelCommand : MikroTikCommand, IMikroTikResponseListener
    {
        // Это свойство требуется интерфейсом но не участвует в синхронизации потоков.
        private readonly object _syncObj = new();
        /// <summary>
        /// Тег операции которую нужно отменить.
        /// </summary>
        public readonly string Tag;
        /// <summary>
        /// Собственный тег
        /// </summary>
        public readonly string SelfTag;
        private readonly TaskCompletionSource<object?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly MikroTikSocket _socket;

        // ctor
        internal MikroTikAsyncCancelCommand(string tag, string selfTag, MikroTikSocket socket) : base("/cancel")
        {
            Tag = tag;
            SelfTag = selfTag;
            _socket = socket;

            Attribute("tag", tag).SetTag(selfTag);
        }

        object IMikroTikResponseListener.SyncObj => _syncObj;

        /// <summary>
        /// Ожидает подтверждения отмены от сервера.
        /// </summary>
        internal Task WaitDoneAsync()
        {
            return _tcs.Task;
        }

        // Эту функцию вызывает поток читающий сокет.
        void IMikroTikResponseListener.Done()
        {
            _tcs.TrySetResult(null);

            // Удалить себя из словаря.
            _socket.RemoveListener(SelfTag);
        }

        // В процессе ожидания подтверждения отмены может произойти обрыв соединения.
        void IMikroTikResponseListener.AddCriticalException(Exception exception)
        {
            OnFatalOrCriticalException(exception);
        }

        void IMikroTikResponseListener.AddFatal(Exception exception)
        {
            OnFatalOrCriticalException(exception);
        }

        private void OnFatalOrCriticalException(Exception exception)
        {
            // Агрегируем исключение что-бы передать его ожидающему потоку.
            _tcs.TrySetException(exception);

            // Удалить себя из словаря.
            _socket.RemoveListener(SelfTag);
        }

        #region Не используемые члены интерфейса

        // Не может произойти.
        void IMikroTikResponseListener.AddResult(MikroTikResponseFrame message)
        {
        }

        // Не может произойти.
        void IMikroTikResponseListener.AddTrap(MikroApiTrapException trapException)
        {
        }

        #endregion
    }
}
