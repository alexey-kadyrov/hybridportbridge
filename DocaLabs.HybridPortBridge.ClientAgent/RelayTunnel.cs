using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.DataChannels;
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
        private readonly SemaphoreSlim _establishLock;
        private RemoteRelayDataChannel _dataChannel;
        private DownlinkPump _downlinkPump;
        private readonly FrameDispatcher _frameDispatcher;
        private readonly TimeSpan _ttl;

        private readonly string _entityPath;
        private static long _dowlinkPumpCounter;

        private DateTime _canAcceptUntil;
        private readonly TunnelPreamble _tunnelPreamble;

        public Action<RelayTunnel> OnDataChannelClosed { get; set; }

        public RelayTunnel(ILogger logger, ServiceNamespaceOptions serviceNamespace, string entityPath, int remoteTcpPort, int ttlSeconds)
        {
            if (string.IsNullOrWhiteSpace(entityPath))
                throw new ArgumentNullException(nameof(entityPath));

            _log = logger.ForContext(GetType());

            _entityPath = entityPath;
            _remoteTcpPort = remoteTcpPort;
            _establishLock = new SemaphoreSlim(1, 1);
            _relay = new UriBuilder("sb", serviceNamespace.ServiceNamespace, -1, _entityPath).Uri;
            _tokenProvider = serviceNamespace.CreateSasTokenProvider();
            _frameDispatcher = new FrameDispatcher(_log);
            _ttl = TimeSpan.FromSeconds(ttlSeconds);
            _tunnelPreamble = new TunnelPreamble(TunnelFlags.Tcp, _remoteTcpPort);
            _canAcceptUntil = DateTime.MaxValue;
        }

        public bool CanStillAccept()
        {
            return DateTime.UtcNow < _canAcceptUntil;
        }

        public async Task EnsureRelayConnection(TcpClient endpoint, ConnectionId connectionId)
        {
            await _establishLock.ExecuteAction(async () =>
            {
                if (_dataChannel != null)
                {
                    _log.Debug("Relay: {relay}. Using established data channel", _relay);
                    return;
                }

                await InitializeDatachannel();
            });

            EnsureUplinkPump(endpoint, connectionId);
        }

        private async Task InitializeDatachannel()
        {
            var dataChannelFactory = new HybridConnectionClient(_relay, _tokenProvider);

            var stream = await dataChannelFactory.CreateConnectionAsync();

            _log.Information("Relay: {relay}. New data channel is established", _relay);

            try
            {
                await _tunnelPreamble.WriteAsync(stream);

                EnsureDownlinkPump(stream);

                _canAcceptUntil = DateTime.UtcNow.Add(_ttl);
            }
            catch (AuthorizationFailedException e)
            {
                _log.Error(e, "Relay: {relay}. Authorization failed", _relay);
                CloseDataChannel();
                throw;
            }
            catch (Exception e)
            {
                _log.Error(e, "Relay: {relay}. Unable to establish data channel", _relay);
                CloseDataChannel();
                throw;
            }
        }

        private void EnsureDownlinkPump(HybridConnectionStream stream)
        {
            var counter = Interlocked.Increment(ref _dowlinkPumpCounter);

            _dataChannel = new RemoteRelayDataChannel(_log, MetricsRegistry.Factory, counter.ToString(), new MetricTags(), stream);

            _downlinkPump = new DownlinkPump(_log, _dataChannel, _frameDispatcher);

            _downlinkPump.RunAsync().ContinueWith(OnDownlinkPumpCompleted);
        }

        private void EnsureUplinkPump(TcpClient endpoint, ConnectionId connectionId)
        {
            var localDataChannel = new LocalTcpDataChannel(_log, MetricsRegistry.Factory, connectionId.ToString(), new MetricTags(), endpoint);

            var uplinkPump = new UplinkPump(_log, connectionId, localDataChannel, _dataChannel);

            _frameDispatcher.AddQueue(connectionId, localDataChannel);

            uplinkPump.RunAsync().ContinueWith(OnUplinkPumpCompleted);
        }

        private async Task OnDownlinkPumpCompleted(Task<DownlinkPump> prev)
        {
            await _establishLock.WaitAsync();

            try
            {
                CloseDataChannel();
            }
            finally
            {
                _establishLock.Release();
            }
        }

        private void OnUplinkPumpCompleted(Task<UplinkPump> prev)
        {
            _frameDispatcher.RemoveQueue(prev.Result.ConnectionId);
        }

        private void CloseDataChannel()
        {
            try
            {
                _log.Information("Relay: {relay}. Closing the data channel", _relay);

                _frameDispatcher.Clear();
                _downlinkPump?.Stop();
                _downlinkPump = null;

                if (_dataChannel != null)
                {
                    _dataChannel.Dispose();
                    _dataChannel = null;
                }

                _canAcceptUntil = DateTime.MaxValue;

                OnDataChannelClosed?.Invoke(this);
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
                CloseDataChannel();
                _establishLock?.Dispose();
            }
            catch
            {
                // intentional
            }
        }
    }
}
