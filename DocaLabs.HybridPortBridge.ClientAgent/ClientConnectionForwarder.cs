using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    public sealed class ClientConnectionForwarder : IDisposable, IConnectionForwarder
    {
        private readonly ILogger _log;
        private readonly int _fromPort;
        private readonly string _bindTo;
        private readonly FirewallRules _firewallRules;
        private readonly RelayTunnelFactory _relayFactory;
        private TcpListener _endpointListener;
        private MeterMetric _acceptedConnections;

        public ClientConnectionForwarder(ILogger logger, ServiceNamespaceOptions serviceNamespace, int fromPort, PortMappingOptions portMappings)
        {
            if (portMappings == null)
                throw new ArgumentNullException(nameof(portMappings));

            _log = logger.ForContext(GetType());

            _firewallRules = new FirewallRules(portMappings);
            _fromPort = fromPort;
            _bindTo = portMappings.BindToAddress;
            _relayFactory = new RelayTunnelFactory(logger, serviceNamespace, portMappings);
        }

        public void Start()
        {
            try
            {
                if (string.IsNullOrEmpty(_bindTo) || !IPAddress.TryParse(_bindTo, out var bindToAddress))
                    bindToAddress = IPAddress.Any;

                _endpointListener = new TcpListener(bindToAddress, _fromPort);

                _endpointListener.Start();

                _endpointListener.AcceptTcpClientAsync().ContinueWith(ClientAccepted);
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to open listener on port {port}", _fromPort);
                throw;
            }
        }

        public void Stop()
        {
            _endpointListener.IgnoreException(x => x.Stop());

            Dispose();
        }

        public void Dispose()
        {
            _log.Information("Closing listener on port {port}", _fromPort);

            _relayFactory.IgnoreException(x => x.Dispose());
        }

        private async Task ClientAccepted(Task<TcpClient> prev)
        {
#pragma warning disable 4014
            _endpointListener.AcceptTcpClientAsync().ContinueWith(ClientAccepted);
#pragma warning restore 4014

            if (prev.Exception != null)
            {
                _log.Error(prev.Exception, "Failure accepting client on port {port}", _fromPort);
                return;
            }

            TcpClient endpoint = null;

            try
            {
                endpoint = prev.Result;

                var remoteIPEndpoint = (IPEndPoint)endpoint.Client.RemoteEndPoint;

                if (!_firewallRules.IsInRange(remoteIPEndpoint))
                {
                    _log.Warning("No matching firewall rule. Dropping connection on port {port} from {remoteIPEndpoint}", _fromPort, remoteIPEndpoint.Address);

                    endpoint.IgnoreException(x => x.Close());
                }
                else
                {
                    _log.Debug("Accepted connection on port {port} from {remoteIPEndpoint}", _fromPort, remoteIPEndpoint.Address);

                    endpoint.NoDelay = true;
                    endpoint.LingerState.Enabled = false;

                    var connectionId = ConnectionId.New();

                    await _relayFactory
                        .Get()
                        .EnsureRelayConnection(endpoint, connectionId);

                    _log.Debug("ConnectionId: {connectionId}. Pumps started on port {port} from {remoteIPEndpoint}", connectionId, _fromPort, remoteIPEndpoint.Address);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Unable to establish connection on port {port}", _fromPort);

                endpoint.IgnoreException(x => x.Close());
            }
        }
    }
}
 