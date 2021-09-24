using System;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroApiDoneException : Exception
    {
        public MikroApiDoneException()
        {

        }

        public MikroApiDoneException(string message) : base(message)
        {

        }
    }
}
