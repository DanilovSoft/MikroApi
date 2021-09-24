using System;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Представляет ошибку в результате успешной отмены операции.
    /// </summary>
    public class MikroApiCommandInterruptedException : MikroApiTrapException
    {
        public MikroApiCommandInterruptedException()
        {
        }

        public MikroApiCommandInterruptedException(string message) : base(message)
        {
        }

        public MikroApiCommandInterruptedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
