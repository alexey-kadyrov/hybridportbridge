using System;
using System.IO;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class TunnelPreamble
    {
        // the first 16 bits are reserved for future flags and the last 16 bits are reserved for a future use
        // as a length of the rest of the preamble's data
        private const int ByteSize = sizeof(ushort) + sizeof(int) + sizeof(ushort);

        public int ConfigurationKey { get; }

        public TunnelPreamble(int configurationKey)
        {
            ConfigurationKey = configurationKey;
        }

        public static async Task<TunnelPreamble> ReadAsync(Stream stream)
        {
            var buffer = new byte[ByteSize];

            var bytesRead = await stream.ReadAsync(buffer, 0, ByteSize);

            if (bytesRead == 0)
                return null;

            while (bytesRead < ByteSize)
            {
                bytesRead += await stream.ReadAsync(buffer, bytesRead, ByteSize - bytesRead);
            }

            var port = BitConverter.ToInt32(buffer, sizeof(ushort));

            return new TunnelPreamble(port);
        }

        public Task WriteAsync(Stream stream)
        {
            var buffer = new byte[ByteSize];

            var bytes = BitConverter.GetBytes(0);
            Buffer.BlockCopy(bytes, 0, buffer, 0, sizeof(ushort));

            bytes = BitConverter.GetBytes(ConfigurationKey);
            Buffer.BlockCopy(bytes, 0, buffer, sizeof(ushort), sizeof(int));

            return stream.WriteAsync(buffer, 0, ByteSize);
        }

        public override string ToString()
        {
            return $"ConfigurationKey={ConfigurationKey}";
        }
    }
}
