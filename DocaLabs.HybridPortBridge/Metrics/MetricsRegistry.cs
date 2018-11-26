﻿using System;
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
        public MetricTags Tags { get; }
        
        private ReportScheduler _reportScheduler;

        private MetricsRegistry(IMetricsRoot metrics, MetricTags tags)
        {
            Tags = MetricTags.Concat(Tags, tags);
            _metrics = metrics;
            _meters = _metrics.Measure.Meter;
        }

        private MetricsRegistry(IMetricsRoot metrics)
        {
            _metrics = metrics;
            _meters = _metrics.Measure.Meter;
        }

        public static MetricsRegistry CreateRoot(IConfiguration configuration, Action<IMetricsBuilder> customBuild = null)
        {
            var options = configuration.GetSection("AgentMetrics")?.Get<AgentMetricsOptions>() ?? new AgentMetricsOptions();

            var builder = new MetricsBuilder()
                .Configuration.Configure(options.MetricsOptions);

            BuildReporting(builder, options.ReportingOptions);

            customBuild?.Invoke(builder);
            
            var registry = new MetricsRegistry(builder.Build());
            
            registry.StartReportScheduler(options);

            return registry;
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
            return new MeterMetric(_meters, options, Tags);
        }

        public void Dispose()
        {
            _reportScheduler?.Stop();
        }
    }
}
