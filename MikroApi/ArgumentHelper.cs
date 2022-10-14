using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MikroApi;

internal static class ArgumentHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateTimeout(int millisecondsTimeout, [CallerArgumentExpression("millisecondsTimeout")] string paramName = "")
    {
        if (millisecondsTimeout >= -1)
        {
            return;
        }

        ThrowArgument(paramName, millisecondsTimeout);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgument(string paramName, object? value)
    {
        throw new ArgumentOutOfRangeException(paramName, value, null);
    }
}
