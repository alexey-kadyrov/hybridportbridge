using App.Metrics;
using App.Metrics.Meter;

namespace DocaLabs.HybridPortBridge.Metrics
{
    public sealed class LocalDataChannelMetrics
    {
        private static readonly MeterOptions LocalFailuresOptions = new MeterOptions
        {
            Name = "Failure (Local)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions LocalFrameReadOptions = new MeterOptions
        {
            Name = "Frame Read (Local)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions LocalFrameWrittenOptions = new MeterOptions
        {
            Name = "Frame Written (Local)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions LocalBytesReadOptions = new MeterOptions
        {
            Name = "Bytes Read (Local)",
            MeasurementUnit = Unit.Bytes
        };

        private static readonly MeterOptions LocalBytesWrittenOptions = new MeterOptions
        {
            Name = "Bytes Written (Local)",
            MeasurementUnit = Unit.Bytes
        };
        
        private readonly MetricsRegistry _registry;
        private readonly MeterMetric _failures;
        private readonly MeterMetric _frameRead;
        private readonly MeterMetric _frameWritten;
        private readonly MeterMetric _bytesRead;
        private readonly MeterMetric _bytesWritten;

        public LocalDataChannelMetrics(MetricsRegistry registry)
        {
            _registry = registry;
            _failures = _registry.MakeMeter(LocalFailuresOptions);
            _frameRead = _registry.MakeMeter(LocalFrameReadOptions);
            _frameWritten = _registry.MakeMeter(LocalFrameWrittenOptions);
            _bytesRead = _registry.MakeMeter(LocalBytesReadOptions);
            _bytesWritten = _registry.MakeMeter(LocalBytesWrittenOptions);
        }

        public LocalDataChannelMetrics Merge(MetricTags tags)
        {
            return new LocalDataChannelMetrics(_registry.Merge(tags));
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

        public override string ToString()
        {
            return _registry.ToString();
        }
    }
}