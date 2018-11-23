using DocaLabs.HybridPortBridge.Metrics;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class RemoteDataChannelMetrics
    {
        private readonly MeterMetric _failures;
        private readonly MeterMetric _frameRead;
        private readonly MeterMetric _frameWritten;
        private readonly MeterMetric _bytesRead;
        private readonly MeterMetric _bytesWritten;

        public RemoteDataChannelMetrics(MetricsRegistry metrics)
        {
            _failures = metrics.MakeMeter(MetricsRegistry.RemoteFailuresOptions);
            _frameRead = metrics.MakeMeter(MetricsRegistry.RemoteFrameReadOptions);
            _frameWritten = metrics.MakeMeter(MetricsRegistry.RemoteFrameWrittenOptions);
            _bytesRead = metrics.MakeMeter(MetricsRegistry.RemoteBytesReadOptions);
            _bytesWritten = metrics.MakeMeter(MetricsRegistry.RemoteBytesWrittenOptions);
        }

        public void FrameWritten(int size)
        {
            _frameWritten.Increment();
            _bytesWritten.Increment(size);
        }

        public void FrameRead(int size)
        {
            _frameRead.Increment();
            _bytesRead.Increment(size);
        }

        public void Failed()
        {
            _failures.Increment();
        }
    }
}