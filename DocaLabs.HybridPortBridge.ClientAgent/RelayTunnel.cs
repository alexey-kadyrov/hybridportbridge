using System;
using System.Collections.Concurrent;
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
        private readonly Uri _relay;
        private readonly TokenProvider _tokenProvider;
        private readonly SemaphoreSlim _establishLock;
        private RemoteRelayDataChannel _dataChannel;
        private DownlinkPump _downlinkPump;
        private readonly ConcurrentDictionary<object, UplinkPump> _uplinkPumps;
        private readonly FrameDispatcher _frameDispatcher;
        private readonly TimeSpan _ttl;
        private DateTime _canAcceptUntil;
        private readonly TunnelPreamble _tunnelPreamble;
        private readonly TunnelMetrics _metrics;

        public Action<RelayTunnel> OnDataChannelClosed { get; set; }

        public RelayTunnel(
            ILogger logger, TunnelMetrics metrics,
            ServiceNamespaceOptions serviceNamespace, string entityPath, int remoteConfigurationKey, int ttlSeconds)
        {
            if (string.IsNullOrWhiteSpace(entityPath))
                throw new ArgumentNullException(nameof(entityPath));

            _log = logger.ForContext(GetType());
            _uplinkPumps = new ConcurrentDictionary<object, UplinkPump>();
            _establishLock = new SemaphoreSlim(1, 1);
            _relay = new UriBuilder("sb", serviceNamespace.ServiceNamespace, -1, entityPath).Uri;
            _tokenProvider = serviceNamespace.CreateSasTokenProvider();
            _frameDispatcher = new FrameDispatcher(_log);
            _ttl = TimeSpan.FromSeconds(ttlSeconds);
            _tunnelPreamble = new TunnelPreamble(remoteConfigurationKey);
            _canAcceptUntil = DateTime.MaxValue;
            _metrics = metrics;
        }

        public bool CanStillAccept()
        {
            return DateTime.UtcNow < _canAcceptUntil;
        }

        public async Task EnsureRelayConnection(TcpClient endpoint, ConnectionId connectionId)
        {
            await _establishLock.WaitAsync();

            try
            {
                if (_dataChannel != null)
                    _log.Debug("Relay: {relay}. Using established data channel", _relay);
                else
                    await InitializeDataChannel();
            }
            finally
            {
                _establishLock.Release();
            }

            EnsureUplinkPump(endpoint, connectionId);
        }

        private async Task InitializeDataChannel()
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
            _dataChannel = new RemoteRelayDataChannel(_log, _metrics.Remote, stream);

            _downlinkPump = new DownlinkPump(_log, _dataChannel, _frameDispatcher);

            _metrics.RemoteEstablishedTunnels.Increment();
            
            _downlinkPump.RunAsync().ContinueWith(OnDownlinkPumpCompleted);
        }

        private void EnsureUplinkPump(TcpClient endpoint, ConnectionId connectionId)
        {
            var localDataChannel = new LocalTcpDataChannel(_log, _metrics.Local, connectionId.ToString(), endpoint);

            var uplinkPump = new UplinkPump(_log, connectionId, localDataChannel, _dataChannel);

            _uplinkPumps[uplinkPump] = uplinkPump;

            _frameDispatcher.AddQueue(connectionId, localDataChannel);

            _metrics.LocalEstablishedConnections.Increment();
            
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
            _log.Verbose("ConnectionId {connectionId}. Uplink pump completed", prev.Result.ConnectionId);

            _frameDispatcher.RemoveQueue(prev.Result.ConnectionId);

            if (_uplinkPumps.TryRemove(prev.Result, out _))
                prev.Result.IgnoreException(x => x.Dispose());
        }

        private void CloseDataChannel()
        {
            try
            {
                _log.Information("Relay: {relay}. Closing the data channel", _relay);

                _frameDispatcher.Dispose();
                _uplinkPumps.DisposeAndClear();
                _downlinkPump?.Stop();
                _downlinkPump = null;

                if (_dataChannel != null)
                {
                    _dataChannel.Dispose();
                    _dataChannel = null;
                }

                _canAcceptUntil = DateTime.MaxValue;

                OnDataChannelClosed?.Invoke(this);

                _log.Debug("Relay: {relay}. The data channel was closed", _relay);
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
