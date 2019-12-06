using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    /*

    /cancel
    =tag=22
    .tag=1

    !trap
    =category=2
    =message=interrupted
    .tag=22

    !done
    .tag=1

    !done
    .tag=22
    
        */

    /// <summary>
    /// Команда "/cancel" служащая для отмены подписки.
    /// </summary>
    internal class MikroTikCancelCommand : MikroTikCommand, IMikroTikResponseListener
    {
        // Это свойство требуется интерфейсом но не участвует в синхронизации потоков.
        private readonly object _syncObj = new object();
        object IMikroTikResponseListener.SyncObj => _syncObj;
        /// <summary>
        /// Тег операции которую нужно отменить.
        /// </summary>
        public readonly string Tag;
        /// <summary>
        /// Собственный тег.
        /// </summary>
        public readonly string SelfTag;
        private readonly MikroTikSocket _socket;
        private volatile Exception _exception;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag">Тег операции которую нужно отменить.</param>
        /// <param name="selfTag">Собственный тег команды.</param>
        /// <param name="socket"></param>
        internal MikroTikCancelCommand(string tag, string selfTag, MikroTikSocket socket) : base("/cancel")
        {
            Tag = tag;
            SelfTag = selfTag;
            _socket = socket;

            Attribute("tag", tag).SetTag(selfTag);
        }

        /// <summary>
        /// Ожидает подтверждения отмены от сервера с тегом <see cref="SelfTag"/>.
        /// </summary>
        internal void Wait()
        {
            // Разрешаем вход другому потоку и ждем пока он сообщит о завершении.
            Monitor.Wait(this);

            // Исключение устанавливается до выхода из блокировки другим потоком.
            Exception ex = _exception;

            if (ex != null)
                throw ex;
        }

        // Эту функцию вызывает поток читающий сокет.
        void IMikroTikResponseListener.Done()
        {
            // Нужно обязательно сменить поток.
            ThreadPool.UnsafeQueueUserWorkItem(state => 
            {
                lock (state)
                {
                    // Сообщаем родителю что работа завершена.
                    Monitor.Pulse(state);
                }
            }, this);

            // Удалить себя из словаря.
            _socket.RemoveListener(SelfTag);
        }

        // В процессе ожидания подтверждения отмены может произойти обрыв соединения.
        private void OnFatalOrCriticalException(Exception exception)
        {
            // Агрегируем исключение что-бы передать его ожидающему потоку.
            _exception = exception;

            // Нужно обязательно сменить поток.
            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                lock (state)
                {
                    // Сообщаем родителю что работа завершена.
                    Monitor.Pulse(state);
                }
            }, this);

            // Удалить себя из словаря.
            _socket.RemoveListener(SelfTag);
        }

        void IMikroTikResponseListener.AddCriticalException(Exception exception)
        {
            OnFatalOrCriticalException(exception);
        }

        void IMikroTikResponseListener.AddFatal(Exception exception)
        {
            OnFatalOrCriticalException(exception);
        }

        #region Не используемые члены интерфейса
        // Не может произойти.
        void IMikroTikResponseListener.AddResult(MikroTikResponseFrame message) { }
        // Не может произойти.
        void IMikroTikResponseListener.AddTrap(MikroTikTrapException trapException) { }
        #endregion
    }
}
