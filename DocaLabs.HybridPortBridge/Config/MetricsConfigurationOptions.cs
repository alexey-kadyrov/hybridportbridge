using System.Collections.Generic;
using App.Metrics;

namespace DocaLabs.HybridPortBridge.Config
{
    public class MetricsConfigurationOptions
    {
        private const string DefaultContext = "Application";

        public string DefaultContextLabel { get; set; } = DefaultContext;

        public Dictionary<string, string> GlobalTags { get; set; } = new Dictionary<string, string>();

        public bool Enabled { get; set; } = true;

        public bool ReportingEnabled { get; set; } = true;

        public MetricsOptions ToMetricsOptions()
        {
            return new MetricsOptions
            {
                DefaultContextLabel = DefaultContextLabel,
                GlobalTags = new GlobalMetricTags(GlobalTags),
                Enabled = Enabled,
                ReportingEnabled = ReportingEnabled
            };
        }
    }
}