using System;
using System.Threading.Tasks;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class UplinkPump : IDisposable
    {
        private readonly ILogger _log;
        private readonly ILocalDataChannelReader _localReader;
        private readonly IRemoteDataChannelWriter _remoteWriter;
        private bool _stopped;

        public ConnectionId ConnectionId { get; }

        public UplinkPump(ILogger logger, ConnectionId connectionId, ILocalDataChannelReader localReader, IRemoteDataChannelWriter remoteWriter)
        {
            _log = logger.ForContext(GetType());

            ConnectionId = connectionId;
            _localReader = localReader;
            _remoteWriter = remoteWriter;
        }

        public void Dispose()
        {
            _localReader.IgnoreException(x => x.Dispose());
        }

        public async Task<UplinkPump> RunAsync()
        {
            _log.Debug("ConnectionId: {connectionId}. Running uplink pump", ConnectionId);

            try
            {
                while (true)
                {
                    var (bytesRead, data) = await _localReader.ReadAsync();
                    if (bytesRead <= 0)
                    {
                        _log.Verbose("ConnectionId: {connectionId}. Read 0 bytes from local, the pump will be stopped", ConnectionId);
                        break;
                    }

                    if (_stopped)
                    {
                        _log.Verbose("ConnectionId: {connectionId}. Uplink pump has been stopped", ConnectionId);
                        return this;
                    }

                    _log.Verbose("ConnectionId: {connectionId}. Read bytes from local: {bytesRead}", ConnectionId, bytesRead);

                    await _remoteWriter.WriteAsync(ConnectionId, data, bytesRead);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "ConnectionId: {connectionId}. Uplink pump failed", ConnectionId);
            }

            _log.Debug("ConnectionId: {connectionId}. Uplink pump completed", ConnectionId);

            _stopped = true;

            return this;
        }
    }
}
