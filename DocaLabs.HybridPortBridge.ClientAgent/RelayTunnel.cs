using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class RelayTunnel : IDisposable
    {
        private readonly ILogger _log;
        private readonly int _remoteTcpPort;
        private readonly Uri _relay;
        private readonly TokenProvider _tokenProvider;
        private readonly SemaphoreSlim _connectLock;
        private HybridConnectionStream _dataChannel;
        private DownlinkPump _downlinkPump;
        private readonly FrameDispatcher _frameDispatcher;
        private readonly TimeSpan _ttl;

        private readonly string _entityPath;
        private MeterMetric _relayWrittenBytes;
        private static long _dowlinkPumpCounter;

        private DateTime _canAcceptUntil;
        private readonly MeterMetric _acceptedConnections;

        public RelayTunnel(ILogger loggerFactory, ServiceNamespaceOptions serviceNamespace, string entityPath, int remoteTcpPort, int ttlSeconds)
        {
            if (string.IsNullOrWhiteSpace(entityPath))
                throw new ArgumentNullException(nameof(entityPath));

            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

            _entityPath = entityPath;
            _remoteTcpPort = remoteTcpPort;
            _connectLock = new SemaphoreSlim(1, 1);
            _relay = new UriBuilder("sb", serviceNamespace.ServiceNamespace, -1, _entityPath).Uri;
            _tokenProvider = serviceNamespace.CreateSasTokenProvider();
            _frameDispatcher = new FrameDispatcher(_log);
            _ttl = TimeSpan.FromSeconds(ttlSeconds);
            _canAcceptUntil = DateTime.MaxValue;
            _acceptedConnections = MetricsRegistry.MakeMeter(MetricsDefinitions.EndpointConnectionAcceptedMeter, _entityPath, _remoteTcpPort);
        }

        public bool CanAcceptNewClients()
        {
            return DateTime.UtcNow < _canAcceptUntil;
        }

        public async Task EnsureRelayConnection(TcpClient endpoint, ConnectionId connectionId)
        {
            _acceptedConnections.Increment();

            await _connectLock.ExecuteAction(async () =>
            {
                if (_dataChannel != null)
                {
                    _log.Debug("Relay: {relay}. Using established data channel", _relay);
                    return;
                }

                await InitializeDatachannel();

                CreateDownlinkPump();
            });

            CreateUplinkPump(endpoint, connectionId);
        }

        private async Task InitializeDatachannel()
        {
            var dataChannelFactory = new HybridConnectionClient(_relay, _tokenProvider);

            _dataChannel = await dataChannelFactory.CreateConnectionAsync();

            _log.Information("Relay: {relay}. New data channel is established", _relay);

            try
            {
                await _dataChannel.WriteLengthPrefixedStringAsync("tcp:" + _remoteTcpPort);
            }
            catch (AuthorizationFailedException e)
            {
                _log.Error(e, "Relay: {relay}. Authorization failed", _relay);
                await CloseDataChannelAsync();
                throw;
            }
            catch (Exception e)
            {
                _log.Error(e, "Relay: {relay}. Unable to establish data channel", _relay);
                await CloseDataChannelAsync();
                throw;
            }

            _canAcceptUntil = DateTime.UtcNow.Add(_ttl);
        }

        private void CreateDownlinkPump()
        {
            var counter = Interlocked.Increment(ref _dowlinkPumpCounter);

            var relayReadBytes = MetricsRegistry.MakeMeter(MetricsDefinitions.RelayBytesReadMeter, _entityPath, _remoteTcpPort, counter);

            _relayWrittenBytes = MetricsRegistry.MakeMeter(MetricsDefinitions.RelayBytesWrittenMeter, _entityPath, _remoteTcpPort, counter);

            _downlinkPump = new DownlinkPump(_log, relayReadBytes, _dataChannel.ReadAsync, _frameDispatcher);

#pragma warning disable 4014
            _downlinkPump.RunAsync().ContinueWith(OnDownlinkPumpCompleted);
#pragma warning restore 4014
        }

        private void CreateUplinkPump(TcpClient endpoint, ConnectionId connectionId)
        {
            var uplinkPump = new UplinkPump(_log, _endpointReadBytes, connectionId, endpoint, WriteToDataChannelAsync);

            var endpointWriter = new EndpointWriter(uplinkPump, endpoint.GetStream().WriteAsync, _endpointWrittentBytes);

            _frameDispatcher.AddQueue(connectionId, endpointWriter);

#pragma warning disable 4014
            uplinkPump.RunAsync().ContinueWith(OnUplinkPumpCompleted);
#pragma warning restore 4014
        }

        private Task OnDownlinkPumpCompleted(Task<DownlinkPump> prev)
        {
            return _connectLock.ExecuteAction(CloseDataChannelAsync);
        }

        private void OnUplinkPumpCompleted(Task<UplinkPump> prev)
        {
            _frameDispatcher.RemoveQueue(prev.Result.ConnectionId);
        }

        private async Task WriteToDataChannelAsync(byte[] buffer, int offset, int count)
        {
            await _connectLock.ExecuteAction(async () =>
            {
                try
                {
                    _relayWrittenBytes?.Increment(count);
                    await _dataChannel.WriteAsync(buffer, offset, count);
                }
                catch (Exception e)
                {
                    _log.Error(e, "Relay: {relay}. Failed writing to data channel", _relay);
                    await CloseDataChannelAsync();
                    throw;
                }
            });
        }

        private async Task CloseDataChannelAsync()
        {
            try
            {
                _log.Information("Relay: {relay}. Closing the data channel", _relay);

                _frameDispatcher.Clear();
                _downlinkPump?.Stop();
                _downlinkPump = null;

                if (_dataChannel != null)
                {
                    await _dataChannel.CloseAsync(new CancellationTokenSource(_dataChannel.WriteTimeout).Token);
                    _relayWrittenBytes = null;
                    _dataChannel = null;
                }

                _canAcceptUntil = DateTime.MaxValue;
            }
            catch
            {
                // intentional
            }
        }

        public void Dispose()
        {
            try
            {
                CloseDataChannelAsync()
                    .GetAwaiter()
                    .GetResult();

                _dataChannel?.Dispose();
                _connectLock?.Dispose();
            }
            catch
            {
                // intentional
            }
        }
    }
}
