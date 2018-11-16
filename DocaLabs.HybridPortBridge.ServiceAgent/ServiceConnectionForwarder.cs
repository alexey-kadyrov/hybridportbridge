using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Meter;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public sealed class ServiceConnectionForwarder : IConnectionForwarder
    {
        private static readonly MeterOptions EstablisheTunnelsOptions = new MeterOptions
        {
            Name = "Established Tunnels (Remote)",
            MeasurementUnit = Unit.Items
        };

        private readonly ILogger _log;
        private readonly int _forwarderIdx;
        private readonly HybridConnectionListener _relayListener;
        private readonly RelayMetadata _metadata;
        private readonly RelayTunnelFactory _tunnelFactory;
        private readonly ConcurrentDictionary<object, RelayTunnel> _tunnels;
        private readonly MeterMetric _establishedTunnels;

        public ServiceConnectionForwarder(ILogger logger, int forwarderIdx, ServiceNamespaceOptions serviceNamespace, string entityPath)
        {
            _log = logger.ForContext(GetType());

            _forwarderIdx = forwarderIdx;

            _tunnels = new ConcurrentDictionary<object, RelayTunnel>();

            var endpointVia = new UriBuilder("sb", serviceNamespace.ServiceNamespace, -1, entityPath).Uri;

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(serviceNamespace.AccessRuleName, serviceNamespace.AccessRuleKey);

            _relayListener = new HybridConnectionListener(endpointVia, tokenProvider);

            _metadata = RelayMetadata.Parse(_relayListener);

            var metricTags = new MetricTags(new[] { nameof(entityPath), nameof(forwarderIdx) }, new[] { entityPath, forwarderIdx.ToString() });

            _establishedTunnels = MetricsRegistry.Factory.MakeMeter(EstablisheTunnelsOptions, metricTags);

            _tunnelFactory = new RelayTunnelFactory(logger, metricTags, _metadata.TargetHost, OnTunnelCompleted);
        }

        public void Start()
        {
            _log.Information("Relay: {idx}:{relay}. Opening relay listener connection", _forwarderIdx, _relayListener.Address);

            try
            {
                _relayListener.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

                _relayListener.AcceptConnectionAsync().ContinueWith(StreamAccepted);
            }
            catch (Exception e)
            {
                _log.Error(e, "Relay: {idx}:{relay}. Unable to connect", _forwarderIdx, _relayListener.Address);
                throw;
            }
        }

        public void Stop()
        {
            _log.Information("Relay: {idx}:{relay}. Closing relay listener connection", _forwarderIdx, _relayListener.Address);

            _tunnels.DisposeAndClear();

            _relayListener.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private async Task StreamAccepted(Task<HybridConnectionStream> prev)
        {
            try
            {
#pragma warning disable 4014
                _relayListener.AcceptConnectionAsync().ContinueWith(StreamAccepted);
#pragma warning restore 4014

                if (prev.Exception != null)
                    throw prev.Exception;

                var relayStream = prev.Result;
                    
                if (relayStream != null)
                {
                    var info = await relayStream.ReadLengthPrefixedStringAsync();

                    if (info.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(info.Substring(4), out var port))
                        {
                            CloseRelayStream(relayStream, "Bad target port format");
                            return;
                        }

                        if (!_metadata.IsPortAllowed(port))
                        {
                            CloseRelayStream(relayStream, $"Incoming connection for port {port} not permitted");
                            return;
                        }

                        _log.Debug("Relay: {idx}:{relay}. Incoming connection for port {port}", _forwarderIdx, _relayListener.Address, port);

                        EstablishTunnel(relayStream, port);
                    }
                    else
                    {
                        CloseRelayStream(relayStream, $"Unable to handle connection for {info}");
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Relay: {idx}:{relay}. Error accepting connection", _forwarderIdx, _relayListener.Address);
            }
        }

        private void EstablishTunnel(HybridConnectionStream stream, int port)
        {
            var tunnel = _tunnelFactory.Create(stream, port);

            _tunnels[tunnel] = tunnel;

            _establishedTunnels.Increment();

            tunnel.Start();
        }

        private Task OnTunnelCompleted(RelayTunnel tunnel)
        {
            if (_tunnels.TryRemove(tunnel, out _))
                tunnel.IgnoreException(x => x.Dispose());

            return Task.CompletedTask;
        }

        private void CloseRelayStream(Stream stream, string reason)
        {
            _log.Warning("Relay: {idx}:{relay}. " + reason, _forwarderIdx, _relayListener.Address);

            stream.IgnoreException(x => x.Dispose());
        }
    }
}