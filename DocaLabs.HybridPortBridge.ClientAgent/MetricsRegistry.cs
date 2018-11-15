using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    public static class MetricsRegistry
    {
        public static MetricsFactory Factory { get; private set; }

        public static void Build(IConfiguration configuration)
        {
            Factory = new MetricsFactory(configuration);
        }
    }
}
