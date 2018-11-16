using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.DataChannels;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal delegate Task TunnelCompleted(RelayTunnel tunnel);
    internal delegate Task<LocalDataChannel> CreateLocalDataChannel(ConnectionId connectionId, int targetPort, MetricTags metricTags);

    internal sealed class RelayTunnel : IDisposable
    {
        private readonly ILogger _log;
        private readonly CreateLocalDataChannel _createDataChannelFactory;
        private readonly TunnelCompleted _tunnelCompleted;
        private DownlinkPump _downlinkPump;
        private readonly ConcurrentDictionary<object, UplinkPump> _uplinkPumps;
        private readonly FrameDispatcher _frameDispatcher;
        private static long _dowlinkPumpCounter;
        private readonly int _targetPort;
        private readonly MetricTags _baseMetricTags;

        public RelayTunnel(ILogger logger, MetricTags baseTags, HybridConnectionStream relayStream, int targetPort, CreateLocalDataChannel createDataChannelFactory, TunnelCompleted tunnelCompleted)
        {
            _log = logger.ForContext(GetType());

            _uplinkPumps = new ConcurrentDictionary<object, UplinkPump>();
            _createDataChannelFactory = createDataChannelFactory;
            _tunnelCompleted = tunnelCompleted;
            _frameDispatcher = new FrameDispatcher(_log, CorrelateLocalWriter);
            _targetPort = targetPort;

            _baseMetricTags = MakeMetricTags(baseTags);

            _downlinkPump = new DownlinkPump(logger, relayReadBytes, relayStream.ReadAsync, _frameDispatcher);
        }

        private static MetricTags MakeMetricTags(MetricTags baseTags)
        {
            var counter = Interlocked.Increment(ref _dowlinkPumpCounter);

            return MetricTags.Concat(baseTags, new MetricTags("instance", counter.ToString()));
        }

        private async Task<ILocalDataChannelWriter> CorrelateLocalWriter(ConnectionId connectionId)
        {
            var localDataChannel = await _createDataChannelFactory(connectionId, _targetPort, _baseMetricTags);

            var uplinkPump = new UplinkPump(_log, connectionId, localDataChannel, null);

            _uplinkPumps[uplinkPump] = uplinkPump;

#pragma warning disable 4014
            uplinkPump.RunAsync().ContinueWith(OnUplinkPumpCompleted);
#pragma warning restore 4014

            return localDataChannel;
        }

        public void Dispose()
        {
            _uplinkPumps.DisposeAndClear();

            CloseDataChannel();
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

                _frameDispatcher.Clear();
                _downlinkPump?.Stop();
                _downlinkPump.IgnoreException(x => x.Dispose());
                _downlinkPump = null;
            }
            catch
            {
                // intentional
            }
        }
    }
}
