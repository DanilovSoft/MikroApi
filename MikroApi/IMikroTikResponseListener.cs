using System;

namespace DanilovSoft.MikroApi
{
    internal interface IMikroTikResponseListener
    {
        object SyncObj { get; }

        /// <summary>
        /// Добавляет результат в коллекцию. Вызывается если получен тегированный фрейм сообщения.
        /// </summary>
        /// <param name="messageFrame"></param>
        void AddResult(MikroTikResponseFrame messageFrame);

        /// <summary>
        /// Добавляет исключение как результат в коллекцию. Вызывается если получено сообщение об ошибке.
        /// </summary>
        void AddTrap(MikroApiTrapException trapException);

        /// <summary>
        /// Добавляет исключение как результат в коллекцию.
        /// </summary>
        void AddFatal(Exception exception);

        /// <summary>
        /// Добавляет исключение как результат в коллекцию. Вызывается если произошел обрыв сокета.
        /// </summary>
        void AddCriticalException(Exception exception);

        /// <summary>
        /// Вызывается если от сервера получен "!done". Значит сервер прекратил отправку сообщений для этого подписчика. Подписчик удаляется из словаря.
        /// </summary>
        void Done();
    }
}