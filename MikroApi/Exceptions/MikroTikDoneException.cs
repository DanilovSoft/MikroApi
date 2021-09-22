using System;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroTikDoneException : Exception
    {
        public MikroTikDoneException()
        {

        }

        public MikroTikDoneException(string message) : base(message)
        {

        }
    }
}
