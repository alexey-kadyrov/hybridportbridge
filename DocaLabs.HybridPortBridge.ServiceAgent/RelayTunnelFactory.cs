using App.Metrics;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class RelayTunnelFactory
    {
        private readonly ILogger _log;
        private readonly MetricsRegistry _metricsRegistry;
        private readonly TunnelCompleted _tunnelCompleted;
        private readonly MetricTags _baseMetricTags;

        public RelayTunnelFactory(ILogger logger, MetricsRegistry metricsRegistry, MetricTags baseMetricTags, TunnelCompleted tunnelCompleted)
        {
            _log = logger.ForContext(GetType());
            _metricsRegistry = metricsRegistry;
            _tunnelCompleted = tunnelCompleted;
            _baseMetricTags = baseMetricTags;

        }

        public RelayTunnel Create(HybridConnectionStream stream, ILocalDataChannelFactory localFactory)
        {
            return new RelayTunnel(_log, _metricsRegistry, _baseMetricTags, stream, localFactory, _tunnelCompleted);
        }
    }
}
