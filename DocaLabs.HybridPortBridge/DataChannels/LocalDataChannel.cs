using System;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public abstract class LocalDataChannel : ILocalDataChannelReader, ILocalDataChannelWriter
    {
        public const int BufferSize = 65536;

        public abstract Task WriteAsync(Frame frame);
        public abstract Task<(int BytesRead, byte[] Data)> ReadAsync();
        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
