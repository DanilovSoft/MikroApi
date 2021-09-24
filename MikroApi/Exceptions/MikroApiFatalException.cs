using System;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Сообщение сервера в результате закрытия соединения.
    /// </summary>
    public class MikroApiFatalException : MikroApiException
    {
        public MikroApiFatalException()
        {

        }

        public MikroApiFatalException(string message) : base(message)
        {

        }

        public MikroApiFatalException(string? message, Exception? innerException) : base(message, innerException)
        {

        }
    }
}
