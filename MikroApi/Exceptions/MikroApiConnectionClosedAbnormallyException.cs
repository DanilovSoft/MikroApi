using System;

namespace DanilovSoft.MikroApi
{
    public class MikroApiConnectionClosedAbnormallyException : Exception
    {
        public MikroApiConnectionClosedAbnormallyException()
        {
        }

        public MikroApiConnectionClosedAbnormallyException(string message) : base(message)
        {
        }

        public MikroApiConnectionClosedAbnormallyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
