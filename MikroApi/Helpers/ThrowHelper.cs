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

        /// <exception cref="MikroTikConnectionException"/>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotConnected()
        {
            throw new MikroTikConnectionException("You are not connected");
        }

        /// <exception cref="MikroTikConnectionException"/>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowAlreadyConnected()
        {
            throw new MikroTikConnectionException("You are already connected");
        }
    }
}
