using System;

namespace MikroApi
{
    public class MikroTikFatalException : Exception
    {
		public MikroTikFatalException(string message) : base(message) { }
	}
}
