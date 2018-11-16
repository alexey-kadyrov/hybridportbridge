using System;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Meter;
using DocaLabs.HybridPortBridge.Config;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.Metrics
{
    public sealed class MetricsFactory : IDisposable
    {
        public static readonly MeterOptions RemoteEstablisheTunnelsOptions = new MeterOptions
        {
            Name = "Established Tunnels (Remote)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions LocalEstablishedConnectionsOptions = new MeterOptions
        {
            Name = "Established Connections (Local)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions LocalFailuresOptions = new MeterOptions
        {
            Name = "Failure (Local)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions LocalFrameReadOptions = new MeterOptions
        {
            Name = "Frame Read (Local)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions LocalFrameWrittenOptions = new MeterOptions
        {
            Name = "Frame Written (Local)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions LocalBytesReadOptions = new MeterOptions
        {
            Name = "Bytes Read (Local)",
            MeasurementUnit = Unit.Bytes
        };

        public static readonly MeterOptions LocalBytesWrittenOptions = new MeterOptions
        {
            Name = "Bytes Written (Local)",
            MeasurementUnit = Unit.Bytes
        };

        public static readonly MeterOptions RemoteFailuresOptions = new MeterOptions
        {
            Name = "Failure (Remote)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions RemoteFrameReadOptions = new MeterOptions
        {
            Name = "Frame Read (Remote)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions RemoteFrameWrittenOptions = new MeterOptions
        {
            Name = "Frame Written (Remote)",
            MeasurementUnit = Unit.Items
        };

        public static readonly MeterOptions RemoteBytesReadOptions = new MeterOptions
        {
            Name = "Bytes Read (Remote)",
            MeasurementUnit = Unit.Bytes
        };

        public static readonly MeterOptions RemoteBytesWrittenOptions = new MeterOptions
        {
            Name = "Bytes Written (Remote)",
            MeasurementUnit = Unit.Bytes
        };

        public IMetricsRoot Metrics { get; }
        public IMeasureCounterMetrics Counters { get; }
        public IMeasureMeterMetrics Meters { get; }

        private ReportScheduler _reportScheduler;

        public MetricsFactory(IConfiguration configuration, Action<IMetricsBuilder> customBuild = null)
        {
            var options = configuration.GetSection("AgentMetrics")
                ?.Get<AgentMetricsOptions>() ?? new AgentMetricsOptions();

            var builder = new MetricsBuilder()
                .Configuration.Configure(options.MetricsOptions);

            BuildReporting(builder, options.ReportingOptions);

            customBuild?.Invoke(builder);

            Metrics = builder.Build();

            Counters = Metrics.Measure.Counter;
            Meters = Metrics.Measure.Meter;

            StartReportScheduler(options);
        }

        private void StartReportScheduler(AgentMetricsOptions options)
        {
            var flushInterval = options.ReportingOptions.ReportingFlushIntervalSeconds <= 0
                ? TimeSpan.FromSeconds(300)
                : TimeSpan.FromSeconds(options.ReportingOptions.ReportingFlushIntervalSeconds);

            _reportScheduler = new ReportScheduler(Metrics, flushInterval);
        }

        private static void BuildReporting(IMetricsBuilder builder, MetricsReportingOptions options)
        {
            if (options.ReportConsole)
                builder.Report.ToConsole();

            if (!String.IsNullOrWhiteSpace(options.ReportFile))
                builder.Report.ToTextFile(options.ReportFile);
        }

        public MeterMetric MakeMeter(MeterOptions options, MetricTags tags)
        {
            return new MeterMetric(Meters, options, tags);
        }

        public void Dispose()
        {
            _reportScheduler?.Stop();
        }
    }
}
