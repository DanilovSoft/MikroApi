using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
