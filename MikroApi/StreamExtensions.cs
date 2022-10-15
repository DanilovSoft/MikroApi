using System;
using System.IO;
using System.Threading.Tasks;
using DanilovSoft.MikroApi.Helpers;

namespace DanilovSoft.MikroApi;

internal static class StreamExtensions
{
    /// <summary>
    /// Считывает строго заданное количество данных.
    /// </summary>
    /// <exception cref="MikroApiDisconnectException"/>
    public static async ValueTask ReadBlockAsync(this Stream stream, Memory<byte> buffer)
    {
        while (buffer.Length > 0)
        {
            var n = await stream.ReadAsync(buffer).ConfigureAwait(false);
            if (n > 0)
            {
                buffer = buffer.Slice(n);
            }
            else
            {
                ThrowHelper.ThrowDisconnected();
            }
        }
    }
}
