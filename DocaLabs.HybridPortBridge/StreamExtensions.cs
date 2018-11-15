using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge
{
    public static class StreamExtensions
    {
        public static Encoding DefaultEncoding = new UTF8Encoding(false, true);

        public static async Task WriteLengthPrefixedStringAsync(this Stream stream, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var bytes = DefaultEncoding.GetBytes(value);

            await Write7BitEncodedIntAsync(stream, bytes.Length);

            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public static async Task<string> ReadLengthPrefixedStringAsync(this Stream stream)
        {
            var capacity = await Read7BitEncodedIntAsync(stream);

            var buffer = new byte[capacity];

            await stream.ReadAsync(buffer, 0, capacity);

            return DefaultEncoding.GetString(buffer, 0, capacity);
        }

        private static async Task Write7BitEncodedIntAsync(Stream stream, int value)
        {
            var num = (uint)value;

            while (num >= 128U)
            {
                await WriteByteAsync(stream, (byte)(num | 128U));
                num >>= 7;
            }

            await WriteByteAsync(stream, (byte)num);
        }

        private static Task WriteByteAsync(Stream stream, byte value)
        {
            return stream.WriteAsync(new[] { value }, 0, 1);
        }

        private static async Task<int> Read7BitEncodedIntAsync(Stream stream)
        {
            var num1 = 0;
            var num2 = 0;

            while (num2 != 35)
            {
                var num3 = (byte) await ReadByteAsync(stream);
                num1 |= (num3 & sbyte.MaxValue) << num2;
                num2 += 7;

                if ((num3 & 128) == 0)
                    return num1;
            }

            throw new FormatException("Bad7BitInt32");
        }

        private static async Task<int> ReadByteAsync(Stream stream)
        {
            var buffer = new byte[1];

            if (await stream.ReadAsync(buffer, 0, 1) == 0)
                return -1;

            return buffer[0];
        }
    }
}