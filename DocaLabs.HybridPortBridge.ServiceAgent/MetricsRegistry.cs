using App.Metrics;
using App.Metrics.Meter;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public static class MetricsRegistry
    {
        public static MetricsDefinitions Instance { get; private set; }

        public static void Build(IConfiguration configuration)
        {
            Instance = new MetricsDefinitions(configuration);
        }

        public static MeterMetric MakeMeter(MeterOptions options, string entityPath, int targetPort, int serviceForwarderIdx, long instance)
        {
            var tags = new MetricTags(
                new[] { nameof(entityPath), nameof(targetPort), nameof(serviceForwarderIdx), nameof(instance) },
                new[] { entityPath, targetPort.ToString(), serviceForwarderIdx.ToString(), instance.ToString() });

            return new MeterMetric(Instance.Meters, options, tags);
        }

        public static MeterMetric MakeMeter(MeterOptions options, string entityPath, int targetPort, int serviceForwarderIdx)
        {
            var tags = new MetricTags(
                new[] { nameof(entityPath), nameof(targetPort), nameof(serviceForwarderIdx) },
                new[] { entityPath, targetPort.ToString(), serviceForwarderIdx.ToString() });

            return new MeterMetric(Instance.Meters, options, tags);
        }
    }
}
