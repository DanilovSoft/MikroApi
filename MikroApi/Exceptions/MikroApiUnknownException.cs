using System;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroTikUnknownLengthException : Exception
    {
        public MikroTikUnknownLengthException()
        {

        }

        public MikroTikUnknownLengthException(string message) : base(message)
        {

        }

        public MikroTikUnknownLengthException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
