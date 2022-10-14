using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi;

internal static class StreamExtensions
{
    /// <summary>
    /// Считывает строго заданное количество данных.
    /// </summary>
    /// <exception cref="MikroApiDisconnectException"/>
    [DebuggerStepThrough]
    public static void ReadBlock(this Stream stream, byte[] buffer, int offset, int count)
    {
        int n;
        while ((count -= n = stream.Read(buffer, offset, count)) > 0)
        {
            if (n != 0)
            {
                offset += n;
            }
            else
            {
                throw new MikroApiDisconnectException();
            }
        }
    }

    /// <summary>
    /// Считывает строго заданное количество данных.
    /// </summary>
    /// <exception cref="MikroApiDisconnectException"/>
    public static async ValueTask ReadBlockAsync(this Stream stream, Memory<byte> buffer)
    {
        while (buffer.Length > 0)
        {
            var n = await stream.ReadAsync(buffer).ConfigureAwait(false);
            if (n != 0)
            {
                buffer = buffer.Slice(n);
            }
            else
            {
                throw new MikroApiDisconnectException();
            }
        }
    }
}
