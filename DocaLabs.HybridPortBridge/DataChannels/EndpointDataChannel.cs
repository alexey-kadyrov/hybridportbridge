using System;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public abstract class EndpointDataChannel : IDisposable
    {
        public abstract Task WriteAsync();
        public abstract Task<int> ReadAsync();
        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
