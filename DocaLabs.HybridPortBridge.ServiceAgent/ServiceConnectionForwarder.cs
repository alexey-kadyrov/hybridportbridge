using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public sealed class ServiceConnectionForwarder : IForwarder
    {
        private static int _idx;

        private readonly ILogger _log;
        private readonly int _forwarderIdx;
        private readonly HybridConnectionListener _relayListener;
        private readonly RelayMetadata _metadata;
        private readonly RelayTunnelFactory _tunnelFactory;
        private readonly ConcurrentDictionary<object, RelayTunnel> _tunnels;
        private readonly MeterMetric _establishedTunnels;

        private ServiceConnectionForwarder(ILogger logger, MetricsRegistry metricsRegistry, int forwarderIdx, HybridConnectionListener listener, RelayMetadata metadata, string entityPath)
        {
            _log = logger.ForContext(GetType());

            _forwarderIdx = forwarderIdx;
            _relayListener = listener;
            _metadata = metadata;

            _tunnels = new ConcurrentDictionary<object, RelayTunnel>();

            var metricTags = new MetricTags(new[] { nameof(entityPath), nameof(forwarderIdx) }, new[] { entityPath, forwarderIdx.ToString() });

            _establishedTunnels = metricsRegistry.MakeMeter(MetricsRegistry.RemoteEstablishedTunnelsOptions, metricTags);

            _tunnelFactory = new RelayTunnelFactory(logger, metricsRegistry, metricTags, OnTunnelCompleted);
        }

        public static async Task<ServiceConnectionForwarder> Create(ILogger logger, MetricsRegistry metricsRegistry, ServiceNamespaceOptions serviceNamespace, string entityPath)
        {
            var forwarderIdx = Interlocked.Increment(ref _idx);

            var endpointVia = new UriBuilder("sb", serviceNamespace.ServiceNamespace, -1, entityPath).Uri;

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(serviceNamespace.AccessRuleName, serviceNamespace.AccessRuleKey);

            var relayListener = new HybridConnectionListener(endpointVia, tokenProvider);

            var metadata = await RelayMetadata.Parse(relayListener);

            return new ServiceConnectionForwarder(logger, metricsRegistry, forwarderIdx, relayListener, metadata, entityPath);
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
                    var preamble = await TunnelPreamble.ReadAsync(relayStream);

                    var localFactory = _metadata.GetDataChannelFactory(preamble.ConfigurationKey);

                    if(localFactory == null)
                    {
                        CloseRelayStream(relayStream, $"Incoming connection for {preamble.ConfigurationKey} is not permitted");
                        return;
                    }

                    _log.Debug("Relay: {idx}:{relay}. Incoming connection for {configurationKey}", _forwarderIdx, _relayListener.Address, preamble.ConfigurationKey);

                    EstablishTunnel(relayStream, localFactory);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Relay: {idx}:{relay}. Error accepting connection", _forwarderIdx, _relayListener.Address);
            }
        }

        private void EstablishTunnel(HybridConnectionStream stream, ILocalDataChannelFactory localFactory)
        {
            var tunnel = _tunnelFactory.Create(stream, localFactory);

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