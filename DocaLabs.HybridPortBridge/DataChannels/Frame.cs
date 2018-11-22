namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class Frame
    {
        public ConnectionId ConnectionId { get; }
        public ushort Size { get; }
        public byte[] Buffer { get; }

        public Frame(ConnectionId connectionId, ushort size, byte[] buffer)
        {
            ConnectionId = connectionId;
            Size = size;
            Buffer = buffer;
        }

        public Frame(ConnectionId connectionId)
        {
            ConnectionId = connectionId;
        }
    }
}
