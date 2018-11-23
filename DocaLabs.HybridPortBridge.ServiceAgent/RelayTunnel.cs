using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal delegate Task TunnelCompleted(RelayTunnel tunnel);

    internal sealed class RelayTunnel : IDisposable
    {
        private readonly ILogger _log;
        private readonly ILocalDataChannelFactory _localDataChannelFactory;
        private readonly TunnelCompleted _tunnelCompleted;
        private readonly DownlinkPump _downlinkPump;
        private readonly RemoteRelayDataChannel _relayDataChannel;
        private readonly ConcurrentDictionary<object, UplinkPump> _uplinkPumps;
        private readonly FrameDispatcher _frameDispatcher;
        private static long _dowlinkPumpCounter;
        private readonly MetricTags _baseMetricTags;
        private readonly MeterMetric _localConnections;
        private readonly MetricsRegistry _metricsRegistry;

        public RelayTunnel(ILogger logger, MetricsRegistry metricsRegistry, MetricTags baseTags, HybridConnectionStream relayStream, ILocalDataChannelFactory localFactory, TunnelCompleted tunnelCompleted)
        {
            _log = logger.ForContext(GetType());

            _uplinkPumps = new ConcurrentDictionary<object, UplinkPump>();
            _localDataChannelFactory = localFactory;
            _tunnelCompleted = tunnelCompleted;
            _frameDispatcher = new FrameDispatcher(_log, CorrelateLocalDataChannel);
            _metricsRegistry = metricsRegistry;
            _baseMetricTags = MakeMetricTags(baseTags);

            _localConnections = _metricsRegistry.MakeMeter(MetricsRegistry.LocalEstablishedConnectionsOptions, _baseMetricTags);
            
            _relayDataChannel = new RemoteRelayDataChannel(logger, metricsRegistry, _baseMetricTags, relayStream);

            _downlinkPump = new DownlinkPump(logger, _relayDataChannel, _frameDispatcher);
        }

        public void Dispose()
        {
            _uplinkPumps.DisposeAndClear();

            CloseDataChannel();
        }

        private static MetricTags MakeMetricTags(MetricTags baseTags)
        {
            var counter = Interlocked.Increment(ref _dowlinkPumpCounter);

            return MetricTags.Concat(baseTags, new MetricTags("instance", counter.ToString()));
        }

        private async Task<ILocalDataChannelWriter> CorrelateLocalDataChannel(ConnectionId connectionId)
        {
            _localConnections.Increment();
            
            var localDataChannel = await _localDataChannelFactory.Create(_log, _metricsRegistry, _baseMetricTags, connectionId);

            var uplinkPump = new UplinkPump(_log, connectionId, localDataChannel, _relayDataChannel);

            _uplinkPumps[uplinkPump] = uplinkPump;

#pragma warning disable 4014
            uplinkPump.RunAsync().ContinueWith(OnUplinkPumpCompleted);
#pragma warning restore 4014

            return localDataChannel;
        }

        public void Start()
        {
            _downlinkPump.RunAsync().ContinueWith(OnDownlinkPumpCompleted);
        }

        private void OnDownlinkPumpCompleted(Task prev)
        {
            _tunnelCompleted(this);
        }

        private void OnUplinkPumpCompleted(Task<UplinkPump> prev)
        {
            var uplinkPump = prev.Result;

            if(_uplinkPumps.TryRemove(uplinkPump, out _))
                uplinkPump.IgnoreException(x => x.Dispose());
            
            _frameDispatcher.RemoveQueue(uplinkPump.ConnectionId);
        }

        private void CloseDataChannel()
        {
            try
            {
                _log.Information("Closing the data channel");

                _frameDispatcher.Drain();
                _downlinkPump?.Stop();
                _downlinkPump.IgnoreException(x => x.Dispose());
            }
            catch
            {
                // intentional
            }
        }
    }
}
