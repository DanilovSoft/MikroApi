using System;
using System.Collections.Generic;
using System.Text;

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
