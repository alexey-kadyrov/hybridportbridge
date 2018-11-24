using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class RelayTunnelFactory
    {
        private readonly ILogger _log;
        private readonly TunnelCompleted _tunnelCompleted;
        private readonly TunnelMetrics _metrics;

        public RelayTunnelFactory(ILogger logger, TunnelMetrics metrics, TunnelCompleted tunnelCompleted)
        {
            _log = logger.ForContext(GetType());
            _metrics = metrics;
            _tunnelCompleted = tunnelCompleted;
        }

        public RelayTunnel Create(HybridConnectionStream stream, ILocalDataChannelFactory localFactory)
        {
            return new RelayTunnel(_log, _metrics, stream, localFactory, _tunnelCompleted);
        }
    }
}
