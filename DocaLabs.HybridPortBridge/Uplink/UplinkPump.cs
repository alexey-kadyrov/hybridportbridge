using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.Uplink
{
    public sealed class UplinkPump : IDisposable
    {
        private readonly ILogger _log;
        private readonly MeterMetric _endpointReadBytes;
        private readonly TcpClient _endpoint;
        private readonly BufferReadAsync _readEndpoint;
        private readonly IRelayDataChannelWriter _relayWriter;
        private readonly byte[] _buffer;
        private bool _stopped;

        public ConnectionId ConnectionId { get; }

        public UplinkPump(ILogger loggerFactory, MeterMetric endpointReadBytes, ConnectionId connectionId, TcpClient endpoint, IRelayDataChannelWriter relayWriter)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

            ConnectionId = connectionId;
            _endpointReadBytes = endpointReadBytes;
            _endpoint = endpoint;
            _readEndpoint = _endpoint.GetStream().ReadAsync;
            _relayWriter = relayWriter;

            _buffer = new byte[Consts.BufferSize];
        }

        public void Dispose()
        {
            _endpoint.IgnoreException(x => x.Dispose());
        }

        public void Stop()
        {
            _stopped = true;
        }

        public async Task<UplinkPump> RunAsync()
        {
            _log.Debug("ConnectionId: {connectionId}. Running uplink pump", ConnectionId);

            try
            {
                int bytesRead;

                while ((bytesRead = await _readEndpoint(_buffer, Frame.PreambleByteSize, Consts.BufferSize - Frame.PreambleByteSize)) > 0)
                {
                    _endpointReadBytes?.Increment(bytesRead);

                    if (_stopped)
                    {
                        _log.Verbose("ConnectionId: {connectionId}. Uplink pump has been stopped", ConnectionId);
                        return this;
                    }

                    _log.Verbose("ConnectionId: {connectionId}. Read bytes from endpoint: {bytesRead}", ConnectionId, bytesRead);

                    await _relayWriter.WriteAsync(ConnectionId, bytesRead, _buffer);
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
