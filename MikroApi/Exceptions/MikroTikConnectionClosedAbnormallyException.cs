using System;

namespace DanilovSoft.MikroApi
{
    public class MikroTikConnectionClosedAbnormallyException : Exception
    {
        public MikroTikConnectionClosedAbnormallyException()
        {
        }

        public MikroTikConnectionClosedAbnormallyException(string message) : base(message)
        {
        }

        public MikroTikConnectionClosedAbnormallyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
