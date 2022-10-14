using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MikroApi.Helpers;

[DebuggerStepThrough]
internal static class ThrowHelper
{
    /// <exception cref="ObjectDisposedException"/>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowConnectionDisposed()
    {
        throw new ObjectDisposedException(typeof(MikroTikConnection).Name);
    }

    /// <exception cref="ObjectDisposedException"/>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowDisposed<T>()
    {
        throw new ObjectDisposedException(typeof(T).Name);
    }

    /// <exception cref="MikroApiConnectionException"/>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MtOpenConnection ThrowNotConnected()
    {
        throw new MikroApiConnectionException("Connection is not open");
    }

    /// <exception cref="MikroApiConnectionException"/>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowAlreadyConnected()
    {
        throw new MikroApiConnectionException("You are already connected");
    }

    /// <exception cref="MikroApiConnectionException"/>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowCommandAlreadySent()
    {
        throw new MikroApiException("This command already has been used once");
    }

    /// <exception cref="InvalidOperationException"/>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowMaxListeners()
    {
        throw new InvalidOperationException("Maximum listeners reached");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowDelegatedError(Exception exception)
    {
        throw exception;
    }

    /// <exception cref="InvalidOperationException"/>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void QuitAlreadyInProcess()
    {
        throw new InvalidOperationException("Quit command already in process");
    }
}
