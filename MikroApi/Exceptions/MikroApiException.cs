using System;

namespace DanilovSoft.MikroApi
{
    public class MikroApiException : Exception
    {
        public MikroApiException()
        {
        }

        public MikroApiException(string? message) : base(message)
        {
        }

        public MikroApiException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
