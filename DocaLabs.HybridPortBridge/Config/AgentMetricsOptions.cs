using App.Metrics;

namespace DocaLabs.HybridPortBridge.Config
{
    public class AgentMetricsOptions
    {
        public MetricsConfigurationOptions MetricsOptions { get; } = new MetricsConfigurationOptions();
        public MetricsReportingOptions ReportingOptions { get; } = new MetricsReportingOptions();
    }
}
