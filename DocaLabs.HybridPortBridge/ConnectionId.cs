using System;

namespace DocaLabs.HybridPortBridge
{
    public struct ConnectionId
    {
        // Guid serialized as bytearray takes 16 bytes
        public const int ByteSize = 16;

        private readonly Guid _id;

        private ConnectionId(Guid id)
        {
            _id = id;
        }

        public static ConnectionId New()
        {
            return new ConnectionId(Guid.NewGuid());
        }

        public static ConnectionId ReadFrom(byte[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Length < ByteSize)
                throw new ArgumentOutOfRangeException(nameof(array));

            var buffer = new byte[ByteSize];

            Buffer.BlockCopy(array, 0, buffer, 0, ByteSize);

            return new ConnectionId(new Guid(buffer));
        }

        public void WriteTo(byte[] buffer)
        {
            Buffer.BlockCopy(_id.ToByteArray(), 0, buffer, 0, ByteSize);
        }

        public override bool Equals(object obj)
        {
            return obj is ConnectionId id && Equals(id);
        }

        private bool Equals(ConnectionId other)
        {
            return _id.Equals(other._id);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public override string ToString()
        {
            return _id.ToString("N");
        }
    }
}
