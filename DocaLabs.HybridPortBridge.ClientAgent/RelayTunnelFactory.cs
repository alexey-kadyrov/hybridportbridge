using System.Collections.Concurrent;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class RelayTunnelFactory
    {
        private readonly ILogger _log;
        private readonly ServiceNamespaceOptions _serviceNamespace;
        private readonly PortMappingOptions _portMappings;
        private readonly object _poolLocker;
        private readonly RelayTunnel[] _tunnels;
        private readonly TunnelMetrics _metrics;
        private readonly ConcurrentDictionary<object, RelayTunnel> _replacedTunnels;
        private long _counter;

        public RelayTunnelFactory(ILogger logger, TunnelMetrics metrics, ServiceNamespaceOptions serviceNamespace, PortMappingOptions portMappings)
        {
            _log = logger?.ForContext(GetType());

            _poolLocker = new object();
            _serviceNamespace = serviceNamespace;
            _portMappings = portMappings;

            _replacedTunnels = new ConcurrentDictionary<object, RelayTunnel>();

            _tunnels = new RelayTunnel[portMappings.RelayChannelCount];

            _metrics = metrics;

            for (var i = 0; i < portMappings.RelayChannelCount; i++)
            {
                _tunnels[i] = new RelayTunnel(logger, _metrics, serviceNamespace, portMappings.EntityPath, portMappings.RemoteConfigurationKey, portMappings.RelayConnectionTtlSeconds);
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

            return _tunnels[idx] = new RelayTunnel(_log, _metrics, _serviceNamespace, _portMappings.EntityPath, _portMappings.RemoteConfigurationKey, _portMappings.RelayConnectionTtlSeconds);
        }

        private void OnReplacedTunnelDataChannelClosed(RelayTunnel tunnel)
        {
            tunnel.OnDataChannelClosed = null;

            _replacedTunnels.TryRemove(tunnel, out _);

            tunnel.IgnoreException(x => x.Dispose());
        }
    }
}
