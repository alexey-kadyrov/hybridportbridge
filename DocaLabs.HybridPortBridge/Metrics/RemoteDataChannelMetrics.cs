using App.Metrics;
using App.Metrics.Meter;

namespace DocaLabs.HybridPortBridge.Metrics
{
    public sealed class RemoteDataChannelMetrics
    {
        private static readonly MeterOptions RemoteFailuresOptions = new MeterOptions
        {
            Name = "Failure (Remote)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions RemoteFrameReadOptions = new MeterOptions
        {
            Name = "Frame Read (Remote)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions RemoteFrameWrittenOptions = new MeterOptions
        {
            Name = "Frame Written (Remote)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions RemoteBytesReadOptions = new MeterOptions
        {
            Name = "Bytes Read (Remote)",
            MeasurementUnit = Unit.Bytes
        };

        private static readonly MeterOptions RemoteBytesWrittenOptions = new MeterOptions
        {
            Name = "Bytes Written (Remote)",
            MeasurementUnit = Unit.Bytes
        };

        private readonly MetricsRegistry _registry;
        private readonly MeterMetric _failures;
        private readonly MeterMetric _frameRead;
        private readonly MeterMetric _frameWritten;
        private readonly MeterMetric _bytesRead;
        private readonly MeterMetric _bytesWritten;

        public RemoteDataChannelMetrics(MetricsRegistry registry)
        {
            _registry = registry;
            _failures = _registry.MakeMeter(RemoteFailuresOptions);
            _frameRead = _registry.MakeMeter(RemoteFrameReadOptions);
            _frameWritten = _registry.MakeMeter(RemoteFrameWrittenOptions);
            _bytesRead = _registry.MakeMeter(RemoteBytesReadOptions);
            _bytesWritten = _registry.MakeMeter(RemoteBytesWrittenOptions);
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