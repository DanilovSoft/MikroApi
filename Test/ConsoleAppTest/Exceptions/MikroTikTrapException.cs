using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
