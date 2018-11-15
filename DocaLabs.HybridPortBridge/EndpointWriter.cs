using System;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Metrics;

namespace DocaLabs.HybridPortBridge
{
    public sealed class EndpointWriter : IDisposable
    {
        private readonly IDisposable _link;
        private readonly BufferWriteAsync _writer;
        private readonly MeterMetric _endpointWrittenBytes;

        public EndpointWriter(IDisposable link, BufferWriteAsync writer, MeterMetric endpointWrittenBytes)
        {
            _link = link;
            _writer = writer;
            _endpointWrittenBytes = endpointWrittenBytes;
        }

        public Task WriteAsync(byte[] buffer, int offset, int count)
        {
            _endpointWrittenBytes?.Increment(count);

            return _writer(buffer, offset, count);
        }

        public void Dispose()
        {
            _link?.Dispose();
        }
    }
}