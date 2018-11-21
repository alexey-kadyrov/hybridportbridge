using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    public sealed class TcpClientConnectionForwarder : IDisposable, IForwarder
    {
        private readonly ILogger _log;
        private readonly int _fromPort;
        private readonly string _bindTo;
        private readonly FirewallRules _firewallRules;
        private readonly RelayTunnelFactory _relayFactory;
        private TcpListener _endpointListener;
        private readonly MeterMetric _acceptedConnections;

        public TcpClientConnectionForwarder(ILogger logger, MetricsRegistry metricsRegistry, ServiceNamespaceOptions serviceNamespace, int fromPort, PortMappingOptions portMappings)
        {
            _log = logger.ForContext(GetType());
            _firewallRules = new FirewallRules(portMappings);
            _fromPort = fromPort;
            _bindTo = portMappings.BindToAddress;

            var metricTags = new MetricTags(
                new [] {"entityPath", "fromPort" }, new [] { portMappings.EntityPath, fromPort.ToString() });

            _acceptedConnections = metricsRegistry.MakeMeter(MetricsRegistry.LocalEstablishedConnectionsOptions, metricTags);

            _relayFactory = new RelayTunnelFactory(logger, metricsRegistry, metricTags, serviceNamespace, portMappings);
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

                    _acceptedConnections.Increment();

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
 