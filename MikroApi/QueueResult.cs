using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace DanilovSoft.MikroApi
{
    /// <summary>
    /// Агрегирует результат или исключение.
    /// </summary>
    internal struct QueueResult<T>
    {
        internal readonly Exception Exception;
        private readonly T _resultValue;
        internal bool Error => (Exception != null);

        // ctor
        internal QueueResult(T result)
        {
            _resultValue = result;
            Exception = null;
        }

        // ctor
        internal QueueResult(Exception exception)
        {
            Exception = exception;
            _resultValue = default;
        }

        /// <summary>
        /// Результат или исключение.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T GetResult()
        {
            if (Exception != null)
                throw Exception;

            return _resultValue;
        }
    }
}
