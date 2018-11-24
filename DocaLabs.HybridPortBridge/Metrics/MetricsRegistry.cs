using System;
using App.Metrics;
using App.Metrics.Meter;
using DocaLabs.HybridPortBridge.Config;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.Metrics
{
    public sealed class MetricsRegistry : IDisposable
    {
        private readonly IMetricsRoot _metrics;
        private readonly IMeasureMeterMetrics _meters;
        private readonly MetricTags _tags;
        
        private ReportScheduler _reportScheduler;
        
        public MetricsRegistry(IConfiguration configuration, Action<IMetricsBuilder> customBuild = null)
        {
            var options = configuration.GetSection("AgentMetrics")
                ?.Get<AgentMetricsOptions>() ?? new AgentMetricsOptions();

            var builder = new MetricsBuilder()
                .Configuration.Configure(options.MetricsOptions);

            BuildReporting(builder, options.ReportingOptions);

            customBuild?.Invoke(builder);

            _metrics = builder.Build();

            _meters = _metrics.Measure.Meter;

            StartReportScheduler(options);
        }

        private MetricsRegistry(IMetricsRoot metrics, MetricTags tags)
        {
            _tags = MetricTags.Concat(_tags, tags);
            _metrics = metrics;
            _meters = _metrics.Measure.Meter;
        }
        
        public MetricsRegistry Merge(MetricTags tags)
        {
            return new MetricsRegistry(_metrics, tags);
        }
        
        private void StartReportScheduler(AgentMetricsOptions options)
        {
            var flushInterval = options.ReportingOptions.ReportingFlushIntervalSeconds <= 0
                ? TimeSpan.FromSeconds(300)
                : TimeSpan.FromSeconds(options.ReportingOptions.ReportingFlushIntervalSeconds);

            _reportScheduler = new ReportScheduler(_metrics, flushInterval);
        }

        private static void BuildReporting(IMetricsBuilder builder, MetricsReportingOptions options)
        {
            if (options.ReportConsole)
                builder.Report.ToConsole();

            if (!string.IsNullOrWhiteSpace(options.ReportFile))
                builder.Report.ToTextFile(options.ReportFile);
        }

        public MeterMetric MakeMeter(MeterOptions options)
        {
            return new MeterMetric(_meters, options, _tags);
        }

        public void Dispose()
        {
            _reportScheduler?.Stop();
        }
    }
}
