using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DanilovSoft.MikroApi.Helpers
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowConnectionDisposed()
        {
            throw new ObjectDisposedException(typeof(MikroTikConnection).Name);
        }

        /// <exception cref="MikroApiConnectionException"/>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotConnected()
        {
            throw new MikroApiConnectionException("You are not connected");
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
            throw new MikrotikExce("This command is already sent");
        }
    }
}
