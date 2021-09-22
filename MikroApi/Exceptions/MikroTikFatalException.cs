using System;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Сообщение сервера в результате закрытия соединения.
    /// </summary>
    [Serializable]
    public class MikroTikFatalException : Exception
    {
        public MikroTikFatalException()
        {

        }

        public MikroTikFatalException(string message) : base(message)
        {

        }

        public MikroTikFatalException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
