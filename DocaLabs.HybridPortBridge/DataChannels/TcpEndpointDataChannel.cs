using System;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class TcpEndpointDataChannel : EndpointDataChannel
    {
        public override Task WriteAsync()
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadAsync()
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}
