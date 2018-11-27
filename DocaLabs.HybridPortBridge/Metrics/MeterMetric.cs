using App.Metrics;
using App.Metrics.Meter;

namespace DocaLabs.HybridPortBridge.Metrics
{
    public class MeterMetric
    {
        private readonly IMeasureMeterMetrics _meters;
        private readonly MeterOptions _options;
        private readonly MetricTags _tags;

        public MeterMetric(IMeasureMeterMetrics meters, MeterOptions options, MetricTags tags)
        {
            _meters = meters;
            _options = options;
            _tags = tags;
        }

        public void Increment(long amount)
        {
            _meters.Mark(_options, _tags, amount);
        }

        public void Increment()
        {
            _meters.Mark(_options, _tags, 1);
        }
    }
}