using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
#if NETSTANDARD2_0
    internal static class ExtensionMethods
    {
        private const string TryGetArrayFail = "MemoryMarshal.TryGetArray returned false.";

        public static void Write(this Stream stream, Memory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray<byte>(memory, out var segment))
            {
                stream.Write(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                throw new InvalidOperationException(TryGetArrayFail);
            }
        }

        public static Task WriteAsync(this Stream stream, Memory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray<byte>(memory, out var segment))
            {
                return stream.WriteAsync(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                throw new InvalidOperationException(TryGetArrayFail);
            }
        }

        public static Task<int> ReadAsync(this Stream stream, ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out var segment))
            {
                return stream.ReadAsync(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                throw new InvalidOperationException(TryGetArrayFail);
            }
        }

        public static string GetString(this Encoding encoding, ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out var segment))
            {
                return encoding.GetString(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                throw new InvalidOperationException(TryGetArrayFail);
            }
        }

        public static void GetBytes(this Encoding encoding, string s, ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out var segment))
            {
                encoding.GetBytes(s, 0, s.Length, segment.Array, segment.Offset);
            }
            else
            {
                throw new InvalidOperationException(TryGetArrayFail);
            }
        }
    }
#endif
}
