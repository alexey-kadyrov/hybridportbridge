using System;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class RelayConnectionPool
    {
        private readonly ServiceNamespaceOptions _serviceNamespace;
        private readonly PortMappingOptions _portMappings;
        private readonly ILogger _log;
        private readonly object _poolLock;
        private readonly RelayConnection[] _connections;
        private long _counter;

        public RelayConnectionPool(ILogger loggerFactory, ServiceNamespaceOptions serviceNamespace, PortMappingOptions portMappings)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

            _poolLock = new object();

            _serviceNamespace = serviceNamespace;
            _portMappings = portMappings;

            _connections = new RelayConnection[portMappings.RelayChannelCount];

            for (var i = 0; i < portMappings.RelayChannelCount; i++)
            {
                _connections[i] = new RelayConnection(loggerFactory, serviceNamespace, portMappings.EntityPath, portMappings.RemoteTcpPort, portMappings.RelayConnectionTtlSeconds);
            }
        }

        public RelayConnection Get()
        {
            lock (_poolLock)
            {
                var n = ++ _counter;

                var id = n % _connections.Length;

                var connection = _connections[id];

                return connection.CanAcceptNewClients() 
                    ? connection 
                    : ReplaceByNewConnection(id);
            }
        }

        private RelayConnection ReplaceByNewConnection(long id)
        {
            _log.Information("Replacing {id} RelayConnection", id);

            return _connections[id] = new RelayConnection(_log, _serviceNamespace, _portMappings.EntityPath, _portMappings.RemoteTcpPort, _portMappings.RelayConnectionTtlSeconds);
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
                connection.IgnoreException(x => x.Dispose());
        }
    }
}
