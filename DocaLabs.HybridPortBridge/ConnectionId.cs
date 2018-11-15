using System;

namespace DocaLabs.HybridPortBridge
{
    public struct ConnectionId
    {
        // Guid serialized as bytearray takes 16 bytes
        public const int ByteSize = 16;

        public Guid Id { get; }

        private ConnectionId(Guid id)
        {
            Id = id;
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

        public int WriteTo(byte[] buffer)
        {
            Buffer.BlockCopy(Id.ToByteArray(), 0, buffer, 0, ByteSize);

            return ByteSize;
        }

        public override bool Equals(object obj)
        {
            return obj is ConnectionId id && Equals(id);
        }

        public bool Equals(ConnectionId other)
        {
            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id.ToString("N");
        }
    }
}
