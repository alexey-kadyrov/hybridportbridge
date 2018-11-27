using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class RemoteRelayDataChannel : IRemoteDataChannelReader, IRemoteDataChannelWriter
    {
        public const int PreambleByteSize = ConnectionId.ByteSize + sizeof(ushort);

        private readonly ILogger _log;
        private readonly RemoteDataChannelMetrics _metrics;
        private readonly SemaphoreSlim _writeLock;
        private readonly HybridConnectionStream _dataChannel;
        private bool _disposed;

        public RemoteRelayDataChannel(ILogger logger, RemoteDataChannelMetrics metrics, HybridConnectionStream dataChannel)
        {
            _log = logger.ForContext(GetType());
            _dataChannel = dataChannel;
            _metrics = metrics;
            _writeLock = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            _log.Debug("Disposing remote relay data channel {relayTags}, was already disposed={wasDisposed}?", _metrics, _disposed);

            _dataChannel.IgnoreException(x => x.Dispose());

            _writeLock.IgnoreException(x => x.Dispose());

            _disposed = true;
        }

        public async Task<Frame> ReadAsync()
        {
            try
            {
                var preamble = await ReadPreambleAsync();

                if (preamble == null)
                    return null;

                if (preamble.FrameSize <= 0)
                {
                    _log.Verbose("ConnectionId: {connectionId}. Received empty frame", preamble.ConnectionId);
                    return new Frame(preamble.ConnectionId);
                }

                // we have to get the frame off the wire irrespective of whether we can dispatch it
                var bytesRead = 0;

                var buffer = new byte[preamble.FrameSize];

                do
                {
                    bytesRead += await _dataChannel.ReadAsync(buffer, bytesRead, preamble.FrameSize - bytesRead);

                } while (bytesRead < preamble.FrameSize);

                _log.Verbose("ConnectionId: {connectionId}. Received frame, size={frameSize}", preamble.ConnectionId, preamble.FrameSize);

                return new Frame(preamble.ConnectionId, preamble.FrameSize, buffer);
            }
            catch (Exception e)
            {
                _metrics.Failed();

                _log.Error(e, "Failed to read");

                throw;
            }
        }

        public async Task WriteAsync(ConnectionId connectionId, byte[] buffer, int count)
        {
            CopyPreamble(connectionId, count, buffer);

            await _writeLock.WaitAsync();

            try
            {
                await _dataChannel.WriteAsync(buffer, 0, PreambleByteSize + count);
            }
            catch (Exception e)
            {
                _metrics.Failed();

                _log.Error(e, "ConnectionId: {connectionId}. Failed to write", connectionId);

                throw;
            }
            finally
            {
                _writeLock.Release();
            }

            _metrics.FrameWritten(count);

            _log.Verbose("ConnectionId: {connectionId}. Wrote bytes: {bytesWritten}, and preamble: {preambleSize}", connectionId, count, PreambleByteSize);
        }

        private async Task<Preamble> ReadPreambleAsync()
        {
            try
            {
                var buffer = new byte[PreambleByteSize];

                var bytesRead = await _dataChannel.ReadAsync(buffer, 0, PreambleByteSize);

                if (bytesRead == 0)
                {
                    _metrics.FrameRead(0);
                    _log.Debug("Received empty frame");
                    return null;
                }

                while (bytesRead < PreambleByteSize)
                {
                    bytesRead += await _dataChannel.ReadAsync(buffer, bytesRead, PreambleByteSize - bytesRead);
                }

                var connectionId = ConnectionId.ReadFrom(buffer);

                var frameSize = BitConverter.ToUInt16(buffer, ConnectionId.ByteSize);

                _metrics.FrameRead(frameSize);

                return new Preamble(connectionId, frameSize);
            }
            catch (Exception e)
            {
                if (e.Find<SocketException>(x => x.ErrorCode.In(10054)) != null)
                {
                    _log.Information("Remote: {relayTags}. {message}", _metrics, e.Message);
                    return null;
                }

                throw;
            }
        }

        private static void CopyPreamble(ConnectionId connectionId, int count, byte[] buffer)
        {
            connectionId.WriteTo(buffer);

            var bytes = BitConverter.GetBytes((ushort)count);

            Buffer.BlockCopy(bytes, 0, buffer, ConnectionId.ByteSize, sizeof(ushort));
        }

        private sealed class Preamble
        {
            public ConnectionId ConnectionId { get; }
            public ushort FrameSize { get; }

            public Preamble(ConnectionId connectionId, ushort frameSize)
            {
                ConnectionId = connectionId;
                FrameSize = frameSize;
            }
        }
    }
}
