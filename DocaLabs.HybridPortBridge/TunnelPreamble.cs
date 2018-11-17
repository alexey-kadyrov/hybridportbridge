using System;
using System.IO;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge
{
    public sealed class TunnelPreamble
    {
        // the last 16 bits are reserved for a future use as a length of the rest of the preamble's data
        public const int ByteSize = sizeof(TunnelFlags) + sizeof(int) + sizeof(ushort);

        public TunnelFlags Flags { get; }
        public int Port { get; }

        public TunnelPreamble(TunnelFlags flags, int port)
        {
            Flags = flags;
            Port = port;
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

            var flags = BitConverter.ToUInt16(buffer, 0);
            var port = BitConverter.ToInt32(buffer, sizeof(TunnelFlags));

            return new TunnelPreamble((TunnelFlags)flags, port);
        }

        public Task WriteAsync(Stream stream)
        {
            var buffer = new byte[ByteSize];

            var bytes = BitConverter.GetBytes((ushort)Flags);
            Buffer.BlockCopy(bytes, 0, buffer, 0, sizeof(ushort));

            bytes = BitConverter.GetBytes(Port);
            Buffer.BlockCopy(bytes, 0, buffer, sizeof(ushort), sizeof(int));

            return stream.WriteAsync(buffer, 0, ByteSize);
        }

        public override string ToString()
        {
            return $"Flags={Flags}, Port={Port}";
        }
    }

    [Flags]
    public enum TunnelFlags : ushort
    {
        None = 0,
        Tcp = 1,
        Encrypted = 4
    }
}
