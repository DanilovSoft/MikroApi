using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikroApi
{
    public class MikroTikFatalException : Exception
    {
		public MikroTikFatalException(string message) : base(message) { }
	}
}
