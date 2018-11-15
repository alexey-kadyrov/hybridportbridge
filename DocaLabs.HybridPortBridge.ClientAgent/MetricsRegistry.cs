using App.Metrics;
using App.Metrics.Meter;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    public static class MetricsRegistry
    {
        public static MetricsDefinitions Instance { get; private set; }

        public static void Build(IConfiguration configuration)
        {
            Instance = new MetricsDefinitions(configuration);
        }

        public static MeterMetric MakeMeter(MeterOptions options, string entityPath, int targetPort, long instance)
        {
            var tags = new MetricTags(
                new[] { nameof(entityPath), nameof(targetPort), nameof(instance) },
                new[] { entityPath, targetPort.ToString(), instance.ToString() });

            return new MeterMetric(Instance.Meters, options, tags);
        }

        public static MeterMetric MakeMeter(MeterOptions options, string entityPath, int targetPort)
        {
            var tags = new MetricTags(
                new[] { nameof(entityPath), nameof(targetPort) },
                new[] { entityPath, targetPort.ToString() });

            return new MeterMetric(Instance.Meters, options, tags);
        }
    }
}
