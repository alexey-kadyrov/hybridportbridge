using System.Net.Sockets;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class RelayTunnelFactory
    {
        private readonly ILogger _log;
        private readonly string _targetHost;
        private readonly TunnelCompleted _tunnelCompleted;
        private readonly MetricTags _baseMetricTags;
        private readonly MeterMetric _localConnections;

        public RelayTunnelFactory(ILogger logger, MetricTags baseMetricTags, string targetHost, TunnelCompleted tunnelCompleted)
        {
            _log = logger.ForContext(GetType());
            _targetHost = targetHost;
            _tunnelCompleted = tunnelCompleted;

            _baseMetricTags = baseMetricTags;

            _localConnections = MetricsRegistry.Factory.MakeMeter(MetricsFactory.LocalEstablishedConnectionsOptions, _baseMetricTags);
        }

        public RelayTunnel Create(HybridConnectionStream stream, int targetPort)
        {
            return new RelayTunnel(_log, _baseMetricTags, stream, targetPort, CreateLocalDataChannel, _tunnelCompleted);
        }

        private async Task<LocalDataChannel> CreateLocalDataChannel(ConnectionId connectionId, int targetPort, MetricTags metricTags)
        {
            _log.Debug("ConnectionId: {connectionId}. Creating new local data channel", connectionId);

            _localConnections.Increment();

            var tcpClient = new TcpClient(AddressFamily.InterNetwork)
            {
                LingerState = { Enabled = true },
                NoDelay = true
            };

            await tcpClient.ConnectAsync(_targetHost, targetPort);

            return new LocalTcpDataChannel(_log, MetricsRegistry.Factory, connectionId.ToString(), metricTags, tcpClient);
        }
    }
}
