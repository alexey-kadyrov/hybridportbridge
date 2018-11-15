namespace DocaLabs.HybridPortBridge
{
    public sealed class Frame
    {
        public ConnectionId ConnectionId { get; }
        public ushort FrameSize { get; }
        public byte[] Buffer { get; }

        public Frame(ConnectionId connectionId, ushort frameSize, byte[] buffer)
        {
            ConnectionId = connectionId;
            FrameSize = frameSize;
            Buffer = buffer;
        }

        public Frame(ConnectionId connectionId)
        {
            ConnectionId = connectionId;
        }
    }
}
