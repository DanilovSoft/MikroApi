using System;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroTikDisconnectException : Exception
    {
        public MikroTikDisconnectException()
        {

        }

        public MikroTikDisconnectException(string message) : base(message)
        {

        }

        public MikroTikDisconnectException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
