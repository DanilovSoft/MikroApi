using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MikroApi;

/// <summary>
/// Агрегирует результат или исключение.
/// </summary>
internal struct QueueResult<T> where T : notnull
{
    internal readonly Exception? _exception;
    private readonly T? _resultValue;

    // ctor
    internal QueueResult(T result)
    {
        _resultValue = result;
        _exception = null;
    }

    // ctor
    internal QueueResult(Exception exception)
    {
        _exception = exception;
        _resultValue = default;
    }

    internal bool Error => _exception != null;

    /// <summary>
    /// Результат или исключение.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T GetResult()
    {
        if (_exception == null)
        {
            Debug.Assert(_resultValue != null);
            return _resultValue;
        }

        return ThrowDelegatedError(_exception);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ThrowDelegatedError(Exception exception)
    {
        throw exception;
    }
}
