using DocaLabs.HybridPortBridge.Metrics;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class LocalDataChannelMetrics
    {
        private readonly MeterMetric _failures;
        private readonly MeterMetric _frameRead;
        private readonly MeterMetric _frameWritten;
        private readonly MeterMetric _bytesRead;
        private readonly MeterMetric _bytesWritten;

        public LocalDataChannelMetrics(MetricsRegistry metrics)
        {
            _failures = metrics.MakeMeter(MetricsRegistry.LocalFailuresOptions);
            _frameRead = metrics.MakeMeter(MetricsRegistry.LocalFrameReadOptions);
            _frameWritten = metrics.MakeMeter(MetricsRegistry.LocalFrameWrittenOptions);
            _bytesRead = metrics.MakeMeter(MetricsRegistry.LocalBytesReadOptions);
            _bytesWritten = metrics.MakeMeter(MetricsRegistry.LocalBytesWrittenOptions);
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