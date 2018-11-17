using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class RemoteRelayDataChannel : IRemoteDataChannelReader, IRemoteDataChannelWriter
    {
        public const int PreambleByteSize = ConnectionId.ByteSize + sizeof(ushort);

        private readonly ILogger _log;
        private readonly MeterMetric _failures;
        private readonly MeterMetric _frameRead;
        private readonly MeterMetric _frameWritten;
        private readonly MeterMetric _bytesRead;
        private readonly MeterMetric _bytesWritten;
        private readonly SemaphoreSlim _writeLock;
        private readonly HybridConnectionStream _dataChannel;

        public RemoteRelayDataChannel(ILogger logger, MetricsFactory metrics, MetricTags tags, HybridConnectionStream dataChannel)
        {
            _log = logger.ForContext(GetType());
            _dataChannel = dataChannel;

            _failures = metrics.MakeMeter(MetricsFactory.RemoteFailuresOptions, tags);
            _frameRead = metrics.MakeMeter(MetricsFactory.RemoteFrameReadOptions, tags);
            _frameWritten = metrics.MakeMeter(MetricsFactory.RemoteFrameWrittenOptions, tags);
            _bytesRead = metrics.MakeMeter(MetricsFactory.RemoteBytesReadOptions, tags);
            _bytesWritten = metrics.MakeMeter(MetricsFactory.RemoteBytesWrittenOptions, tags);

            _writeLock = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            _dataChannel.IgnoreException(x => x.Dispose());

            _writeLock.IgnoreException(x => x.Dispose());
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
                _failures.Increment();

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
                _failures.Increment();

                _log.Error(e, "ConnectionId: {connectionId}. Failed to write", connectionId);

                throw;
            }
            finally
            {
                _writeLock.Release();
            }

            _frameWritten.Increment();

            _bytesWritten.Increment(count);

            _log.Verbose("ConnectionId: {connectionId}. Wrote bytes: {bytesWritten}, and preamble: {preambleSize}", connectionId, count, PreambleByteSize);
        }

        private async Task<Preamble> ReadPreambleAsync()
        {
            var buffer = new byte[PreambleByteSize];

            var bytesRead = await _dataChannel.ReadAsync(buffer, 0, PreambleByteSize);

            _frameRead.Increment();

            if (bytesRead == 0)
            {
                _log.Debug("Received empty frame");
                return null;
            }

            while (bytesRead < PreambleByteSize)
            {
                bytesRead += await _dataChannel.ReadAsync(buffer, bytesRead, PreambleByteSize - bytesRead);
            }

            var connectionId = ConnectionId.ReadFrom(buffer);

            var frameSize = BitConverter.ToUInt16(buffer, ConnectionId.ByteSize);

            _bytesRead.Increment(PreambleByteSize + frameSize);

            return new Preamble(connectionId, frameSize);
        }

        public static void CopyPreamble(ConnectionId connectionId, int count, byte[] buffer)
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
