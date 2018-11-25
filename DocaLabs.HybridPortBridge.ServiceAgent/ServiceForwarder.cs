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
    public sealed class ServiceForwarder : IForwarder
    {
        private static int _idx;

        private readonly ILogger _log;
        private readonly int _forwarderIdx;
        private readonly HybridConnectionListener _relayListener;
        private readonly RelayMetadata _metadata;
        private readonly RelayTunnelFactory _tunnelFactory;

        private ServiceForwarder(ILogger logger, MetricsRegistry registry, int forwarderIdx, HybridConnectionListener listener, RelayMetadata metadata, string entityPath)
        {
            _log = logger.ForContext(GetType());

            _forwarderIdx = forwarderIdx;
            _relayListener = listener;
            _metadata = metadata;

            var tags = new MetricTags(nameof(entityPath), entityPath);

            var metrics = new TunnelMetrics(registry, tags);
            
            _tunnelFactory = new RelayTunnelFactory(logger, metrics);
        }

        public static async Task<ServiceForwarder> Create(ILogger logger, MetricsRegistry metricsRegistry, ServiceNamespaceOptions serviceNamespace, string entityPath)
        {
            var forwarderIdx = Interlocked.Increment(ref _idx);

            var endpointVia = new UriBuilder("sb", serviceNamespace.ServiceNamespace, -1, entityPath).Uri;

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(serviceNamespace.AccessRuleName, serviceNamespace.AccessRuleKey);

            var relayListener = new HybridConnectionListener(endpointVia, tokenProvider);

            var metadata = await RelayMetadata.Parse(relayListener);

            return new ServiceForwarder(logger, metricsRegistry, forwarderIdx, relayListener, metadata, entityPath);
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

            _tunnelFactory.Dispose();

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

                    var factory = _metadata.GetLocalDataChannelFactory(preamble.ConfigurationKey);

                    if(factory == null)
                    {
                        CloseRelayStream(relayStream, $"Incoming connection for {preamble.ConfigurationKey} is not permitted");
                        return;
                    }

                    _log.Debug("Relay: {idx}:{relay}. Incoming connection for {configurationKey}", _forwarderIdx, _relayListener.Address, preamble.ConfigurationKey);

                    EstablishTunnel(relayStream, factory);
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

            tunnel.Start();
        }

        private void CloseRelayStream(Stream stream, string reason)
        {
            _log.Warning("Relay: {idx}:{relay}. " + reason, _forwarderIdx, _relayListener.Address);

            stream.IgnoreException(x => x.Dispose());
        }
    }
}