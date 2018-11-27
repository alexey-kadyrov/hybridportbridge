using App.Metrics;

namespace DocaLabs.HybridPortBridge.Config
{
    public class AgentMetricsOptions
    {
        public MetricsOptions MetricsOptions { get; } = new MetricsOptions();
        public MetricsReportingOptions ReportingOptions { get; } = new MetricsReportingOptions();
    }
}
