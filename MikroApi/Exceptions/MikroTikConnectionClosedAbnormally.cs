using System;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroTikConnectionClosedAbnormally : Exception
    {
        public MikroTikConnectionClosedAbnormally()
        {

        }

        public MikroTikConnectionClosedAbnormally(string message) : base(message)
        {

        }

        public MikroTikConnectionClosedAbnormally(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
