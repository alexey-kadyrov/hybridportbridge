using System.Collections.Concurrent;
using App.Metrics;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class RelayTunnelFactory
    {
        private readonly MetricsRegistry _metricsRegistry;
        private readonly ServiceNamespaceOptions _serviceNamespace;
        private readonly PortMappingOptions _portMappings;
        private readonly ILogger _log;
        private readonly object _poolLocker;
        private readonly RelayTunnel[] _tunnels;
        private readonly MetricTags[] _metricTags;
        private readonly ConcurrentDictionary<object, RelayTunnel> _replacedTunnels;
        private long _counter;

        public RelayTunnelFactory(ILogger logger, MetricsRegistry metricsRegistry, MetricTags metricTags, ServiceNamespaceOptions serviceNamespace, PortMappingOptions portMappings)
        {
            _log = logger?.ForContext(GetType());

            _poolLocker = new object();
            _metricsRegistry = metricsRegistry;
            _serviceNamespace = serviceNamespace;
            _portMappings = portMappings;

            _replacedTunnels = new ConcurrentDictionary<object, RelayTunnel>();

            _tunnels = new RelayTunnel[portMappings.RelayChannelCount];
            _metricTags = new MetricTags[portMappings.RelayChannelCount];

            for (var i = 0; i < portMappings.RelayChannelCount; i++)
            {
                _metricTags[i] = MetricTags.Concat(metricTags, new MetricTags("idx", i.ToString()));
                _tunnels[i] = new RelayTunnel(logger, _metricsRegistry, _metricTags[i], serviceNamespace, portMappings.EntityPath, portMappings.RemoteTcpPort, portMappings.RelayConnectionTtlSeconds);
            }
        }

        public void Dispose()
        {
            foreach (var connection in _tunnels)
                connection.IgnoreException(x => x.Dispose());

            _replacedTunnels.DisposeAndClear();
        }

        public RelayTunnel Get()
        {
            lock (_poolLocker)
            {
                var n = ++ _counter;

                var idx = n % _tunnels.Length;

                var connection = _tunnels[idx];

                return connection.CanStillAccept() 
                    ? connection 
                    : ReplaceByNewConnection(connection, idx);
            }
        }

        private RelayTunnel ReplaceByNewConnection(RelayTunnel replacing, long idx)
        {
            _log.Information("Replacing {idx} Relay Tunnel", idx);

            replacing.OnDataChannelClosed = OnReplacedTunnelDataChannelClosed;

            return _tunnels[idx] = new RelayTunnel(_log, _metricsRegistry, _metricTags[idx], _serviceNamespace, _portMappings.EntityPath, _portMappings.RemoteTcpPort, _portMappings.RelayConnectionTtlSeconds);
        }

        private void OnReplacedTunnelDataChannelClosed(RelayTunnel tunnel)
        {
            tunnel.OnDataChannelClosed = null;

            _replacedTunnels.TryRemove(tunnel, out _);

            tunnel.IgnoreException(x => x.Dispose());
        }
    }
}
