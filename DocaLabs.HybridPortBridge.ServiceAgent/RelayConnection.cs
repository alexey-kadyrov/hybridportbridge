using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Downlink;
using DocaLabs.HybridPortBridge.Metrics;
using DocaLabs.HybridPortBridge.Uplink;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal delegate Task RelayConnectionCompleted(RelayConnection connection);

    internal sealed class RelayConnection : IDisposable
    {
        private readonly ILogger _log;
        private readonly HybridConnectionStream _relayStream;
        private readonly string _targetHost;
        private readonly int _targetPort;
        private readonly RelayConnectionCompleted _relayCompleted;
        private readonly SemaphoreSlim _connectLock;
        private DownlinkPump _downlinkPump;
        private readonly FrameDispatcher _frameDispatcher;
        private readonly MeterMetric _relayWrittenBytes;
        private static long _dowlinkPumpCounter;
        private readonly MeterMetric _relayConnectionAccepted;
        private readonly MeterMetric _endpointReadBytes;
        private readonly MeterMetric _endpointWrittentBytes;

        public RelayConnection(ILogger loggerFactory, int forwarderIdx, string entityPath, HybridConnectionStream relayStream, string targetHost, int targetPort, RelayConnectionCompleted relayCompleted)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

            _connectLock = new SemaphoreSlim(1, 1);
            _relayStream = relayStream;
            _targetHost = targetHost;
            _targetPort = targetPort;
            _relayCompleted = relayCompleted;
            _frameDispatcher = new FrameDispatcher(_log, CorrelateFrameWriter);

            var counter = Interlocked.Increment(ref _dowlinkPumpCounter);

            var relayReadBytes = MetricsRegistry.MakeMeter(MetricsDefinitions.RelayBytesReadMeter, entityPath, _targetPort, forwarderIdx, counter);

            _relayWrittenBytes = MetricsRegistry.MakeMeter(MetricsDefinitions.RelayBytesWrittenMeter, entityPath, _targetPort, forwarderIdx, counter);

            _downlinkPump = new DownlinkPump(loggerFactory, relayReadBytes, relayStream.ReadAsync, _frameDispatcher);

            _relayConnectionAccepted = MetricsRegistry.MakeMeter(MetricsDefinitions.RelayConnectionAcceptedMeter, entityPath, _targetPort, forwarderIdx);
            _endpointReadBytes = MetricsRegistry.MakeMeter(MetricsDefinitions.EndpointBytesReadMeter, entityPath, _targetPort, forwarderIdx);
            _endpointWrittentBytes = MetricsRegistry.MakeMeter(MetricsDefinitions.EndpointBytesWrittenMeter, entityPath, _targetPort, forwarderIdx);
        }

        private async Task<EndpointWriter> CorrelateFrameWriter(ConnectionId connectionId)
        {
            _log.Debug("ConnectionId: {connectionId}. Initializing new uplink pump", connectionId);

            _relayConnectionAccepted.Increment();

            var tcpClient = new TcpClient(AddressFamily.InterNetwork)
            {
                LingerState = { Enabled = true },
                NoDelay = true
            };

            await tcpClient.ConnectAsync(_targetHost, _targetPort);

            var pump = new UplinkPump(_log, _endpointReadBytes, connectionId, tcpClient, WriteToDataChannelAsync);

            BufferWriteAsync writeBack = tcpClient.GetStream().WriteAsync;

            var writer = new EndpointWriter(pump, writeBack, _endpointWrittentBytes);

#pragma warning disable 4014
            pump.RunAsync().ContinueWith(OnUplinkPumpCompleted);
#pragma warning restore 4014

            return writer;
        }

        public void Dispose()
        {
            CloseDataChannel();

            _connectLock.Dispose();
        }

        public void Start()
        {
            _downlinkPump.RunAsync().ContinueWith(OnDownlinkPumpCompleted);
        }

        private void OnDownlinkPumpCompleted(Task prev)
        {
            _relayCompleted(this);
        }

        private async Task WriteToDataChannelAsync(byte[] buffer, int offset, int count)
        {
            await _connectLock.ExecuteAction(async () =>
            {
                try
                {
                    _relayWrittenBytes.Increment(count);

                    await _relayStream.WriteAsync(buffer, offset, count);
                }
                catch (Exception e)
                {
                    _log.Error(e, "Failed writing to the relay");
                    CloseDataChannel();
                    throw;
                }
            });
        }

        private void OnUplinkPumpCompleted(Task<UplinkPump> prev)
        {
            _frameDispatcher.RemoveQueue(prev.Result.ConnectionId);
        }

        private void CloseDataChannel()
        {
            try
            {
                _log.Information("Closing the data channel");

                _frameDispatcher.Clear();
                _downlinkPump?.Stop();
                _downlinkPump = null;

                _relayStream.IgnoreException(async x => await x.CloseAsync(default(CancellationToken)));
            }
            catch
            {
                // intentional
            }
        }
    }
}
