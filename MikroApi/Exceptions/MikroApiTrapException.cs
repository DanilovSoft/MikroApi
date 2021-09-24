using System;
using System.Globalization;

namespace DanilovSoft.MikroApi
{
    [Serializable]
    public class MikroApiTrapException : MikroApiException
    {
        public MikroApiTrapException()
        {
        }

        public MikroApiTrapException(string? message) : base(message)
        {
        }

        public MikroApiTrapException(MikroTikResponseFrame frame) : base(message: GetMessage(frame))
        {
            if (frame.TryGetValue("category", out var v))
            {
                Category = (TrapCategory)int.Parse(v, CultureInfo.InvariantCulture);
            }
        }

        public TrapCategory? Category { get; }

        private static string? GetMessage(MikroTikResponseFrame frame)
        {
            if (!frame.TryGetValue("message", out var message))
            {
                message = null;
            }

            return message;
        }

        public MikroApiTrapException(TrapCategory? category, string message) : base(message)
        {
            Category = category;
        }

        public MikroApiTrapException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public MikroApiTrapException(TrapCategory? category, string message, Exception innerException) : base(message, innerException)
        {
            Category = category;
        }
    }
}
