using System;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroApiDisconnectException : Exception
    {
        public MikroApiDisconnectException()
        {

        }

        public MikroApiDisconnectException(string message) : base(message)
        {

        }

        public MikroApiDisconnectException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
