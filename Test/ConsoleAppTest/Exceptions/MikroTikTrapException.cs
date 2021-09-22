using System;

namespace MikroApi
{
    public class MikroTikTrapException : Exception
    {
        public TrapCategory? Category { get; }

		public MikroTikTrapException(TrapCategory? category, string message)
			: base(message)
		{
			Category = category;
		}
	}
}
