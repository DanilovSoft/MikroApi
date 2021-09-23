using System;

namespace DanilovSoft.MikroApi
{
    public class MikroTikConnectionException : Exception
    {
        public MikroTikConnectionException()
        {
        }

        public MikroTikConnectionException(string message) : base(message)
        {
        }

        public MikroTikConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
