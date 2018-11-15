using System;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class RelayTunnelPool
    {
        private readonly ServiceNamespaceOptions _serviceNamespace;
        private readonly PortMappingOptions _portMappings;
        private readonly ILogger _log;
        private readonly object _poolLock;
        private readonly RelayTunnel[] _tunnels;
        private long _counter;

        public RelayTunnelPool(ILogger loggerFactory, ServiceNamespaceOptions serviceNamespace, PortMappingOptions portMappings)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

            _poolLock = new object();

            _serviceNamespace = serviceNamespace;
            _portMappings = portMappings;

            _tunnels = new RelayTunnel[portMappings.RelayChannelCount];

            for (var i = 0; i < portMappings.RelayChannelCount; i++)
            {
                _tunnels[i] = new RelayTunnel(loggerFactory, serviceNamespace, portMappings.EntityPath, portMappings.RemoteTcpPort, portMappings.RelayConnectionTtlSeconds);
            }
        }

        public RelayTunnel Get()
        {
            lock (_poolLock)
            {
                var n = ++ _counter;

                var id = n % _tunnels.Length;

                var connection = _tunnels[id];

                return connection.CanAcceptNewClients() 
                    ? connection 
                    : ReplaceByNewConnection(id);
            }
        }

        private RelayTunnel ReplaceByNewConnection(long id)
        {
            _log.Information("Replacing {id} RelayConnection", id);

            return _tunnels[id] = new RelayTunnel(_log, _serviceNamespace, _portMappings.EntityPath, _portMappings.RemoteTcpPort, _portMappings.RelayConnectionTtlSeconds);
        }

        public void Dispose()
        {
            foreach (var connection in _tunnels)
                connection.IgnoreException(x => x.Dispose());
        }
    }
}
