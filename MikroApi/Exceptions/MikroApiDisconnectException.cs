using System;

namespace DanilovSoft.MikroApi
{
    public class MikroApiDisconnectException : MikroApiException
    {
        public MikroApiDisconnectException()
        {
        }

        public MikroApiDisconnectException(string message) : base(message)
        {
        }

        public MikroApiDisconnectException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
