using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroTikTrapException : Exception
    {
        public TrapCategory? Category { get; }

        public MikroTikTrapException()
        {

        }

        public MikroTikTrapException(string message) : base(message)
        {

        }

        public MikroTikTrapException(MikroTikResponseFrame frame) : base(message: GetMessage(frame))
        {
            if (frame.TryGetValue("category", out string v))
                Category = (TrapCategory)int.Parse(v);
        }

        private static string GetMessage(MikroTikResponseFrame frame)
        {
            if (!frame.TryGetValue("message", out string message))
                message = null;

            return message;
        }

        public MikroTikTrapException(TrapCategory? category, string message) : base(message)
		{
			Category = category;
		}

        public MikroTikTrapException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public MikroTikTrapException(TrapCategory? category, string message, Exception innerException) : base(message, innerException)
        {
            Category = category;
        }
    }
}
