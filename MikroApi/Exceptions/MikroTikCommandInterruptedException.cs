using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Представляет ошибку в результате успешной отмены операции.
    /// </summary>
    [Serializable]
    public class MikroTikCommandInterruptedException : MikroTikTrapException
    {
        public MikroTikCommandInterruptedException()
        {

        }

        public MikroTikCommandInterruptedException(string message) : base(message)
        {

        }

        public MikroTikCommandInterruptedException(string message, Exception innerException)
        {

        }
    }
}
