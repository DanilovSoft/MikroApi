using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    internal static class StreamExtensions
    {
        /// <summary>
        /// Считывает строго заданное количество данных.
        /// </summary>
        /// <exception cref="MikroTikDisconnectException"/>
        [DebuggerStepThrough]
        public static void ReadBlock(this Stream stream, byte[] buffer, int offset, int count)
        {
            int n;
            while ((count -= n = stream.Read(buffer, offset, count)) > 0)
            {
                if (n != 0)
                    offset += n;
                else
                    throw new MikroTikDisconnectException();
            }
        }

        /// <summary>
        /// Считывает строго заданное количество данных.
        /// </summary>
        /// <exception cref="MikroTikDisconnectException"/>
        //[DebuggerStepThrough]
        public static async ValueTask ReadBlockAsync(this Stream stream, Memory<byte> buffer)
        {
            while (buffer.Length > 0)
            {
                int n = await stream.ReadAsync(buffer).ConfigureAwait(false);
                if (n != 0)
                    buffer = buffer.Slice(n);
                else
                    throw new MikroTikDisconnectException();
            }
        }

        ///// <summary>
        ///// Считывает строго заданное количество данных.
        ///// </summary>
        ///// <exception cref="MikroTikDisconnectException"/>
        //[DebuggerHidden]
        //public static async Task ReadBlockAsync(this Stream stream, Memory<byte> buffer, int offset, int count, CancellationToken cancellationToken)
        //{
        //    int n;
        //    while ((count -= n = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false)) > 0)
        //    {
        //        if (n == 0)
        //            throw new MikroTikDisconnectException();

        //        offset += n;
        //    }
        //}

        //private static Task<int> ReadAsync(this Stream stream, Memory<byte> buffer, int offset, int count, CancellationToken cancellationToken = default)
        //{
        //    byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(count);
        //    return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, count, cancellationToken), sharedBuffer, buffer);

        //    async Task<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
        //    {
        //        try
        //        {
        //            int result = await readTask.ConfigureAwait(false);
        //            new Span<byte>(localBuffer, 0, result).CopyTo(localDestination.Slice(offset).Span);
        //            return result;
        //        }
        //        finally
        //        {
        //            ArrayPool<byte>.Shared.Return(localBuffer);
        //        }
        //    }
        //}
    }
}
