using App.Metrics;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public static class MetricsRegistry
    {
        public static MetricsFactory Factory { get; private set; }

        public static void Build(IConfiguration configuration)
        {
            Factory = new MetricsFactory(configuration);
        }

        public static MetricTags MakeTags(string entityPath, int targetPort, int serviceForwarderIdx, long instance)
        {
            return new MetricTags(
                new[] { nameof(entityPath), nameof(targetPort), nameof(serviceForwarderIdx), nameof(instance) },
                new[] { entityPath, targetPort.ToString(), serviceForwarderIdx.ToString(), instance.ToString() });
        }

        public static MetricTags MakeTags(string entityPath, int targetPort, int serviceForwarderIdx)
        {
            return new MetricTags(
                new[] { nameof(entityPath), nameof(targetPort), nameof(serviceForwarderIdx) },
                new[] { entityPath, targetPort.ToString(), serviceForwarderIdx.ToString() });
        }
    }
}
