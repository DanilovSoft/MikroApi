using System;

namespace DanilovSoft.MikroApi
{
    public class MikroTikUnknownLengthException : MikroApiException
    {
        public MikroTikUnknownLengthException()
        {
        }

        public MikroTikUnknownLengthException(string? message) : base(message)
        {
        }

        public MikroTikUnknownLengthException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
