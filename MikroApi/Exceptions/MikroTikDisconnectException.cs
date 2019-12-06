using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
