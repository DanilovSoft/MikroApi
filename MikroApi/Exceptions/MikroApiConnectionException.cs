using System;

namespace DanilovSoft.MikroApi
{
    public class MikroApiConnectionException : Exception
    {
        public MikroApiConnectionException()
        {
        }

        public MikroApiConnectionException(string message) : base(message)
        {
        }

        public MikroApiConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
