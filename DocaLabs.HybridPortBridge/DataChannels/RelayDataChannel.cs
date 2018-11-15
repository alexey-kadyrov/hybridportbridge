using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Meter;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class RelayDataChannel : IRelayDataChannelReader, IRelayDataChannelWriter, IDisposable
    {
        public const int PreambleByteSize = ConnectionId.ByteSize + sizeof(ushort);

        private static readonly MeterOptions RelayFailedOptions = new MeterOptions
        {
            Name = "Failure (Relay)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions FrameReadOptions = new MeterOptions
        {
            Name = "Frame Read (Relay)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions FrameWrittenOptions = new MeterOptions
        {
            Name = "Frame Written (Relay)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions BytesReadOptions = new MeterOptions
        {
            Name = "Bytes Read (Relay)",
            MeasurementUnit = Unit.Bytes
        };

        private static readonly MeterOptions BytesWrittenOptions = new MeterOptions
        {
            Name = "Bytes Written (Relay)",
            MeasurementUnit = Unit.Bytes
        };

        private readonly ILogger _log;
        private readonly MeterMetric _relayFailed;
        private readonly MeterMetric _frameRead;
        private readonly MeterMetric _frameWritten;
        private readonly MeterMetric _bytesRead;
        private readonly MeterMetric _bytesWritten;
        private readonly SemaphoreSlim _writeLock;
        private readonly string _instance;
        private readonly HybridConnectionStream _dataChannel;

        public RelayDataChannel(ILogger logger, MetricsFactory metrics, string instance, MetricTags tags, HybridConnectionStream dataChannel)
        {
            _log = logger.ForContext(GetType());
            _instance = instance;
            _dataChannel = dataChannel;

            _relayFailed = metrics.MakeMeter(RelayFailedOptions, tags);
            _frameRead = metrics.MakeMeter(FrameReadOptions, tags);
            _frameWritten = metrics.MakeMeter(FrameWrittenOptions, tags);
            _bytesRead = metrics.MakeMeter(BytesReadOptions, tags);
            _bytesWritten = metrics.MakeMeter(BytesWrittenOptions, tags);

            _writeLock = new SemaphoreSlim(1, 1);

            _log.Debug("Relay: {relay} is initialized", _instance);
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
                _relayFailed.Increment();

                _log.Error(e, "Relay: {relay}. Failed to read", _instance);

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
                _relayFailed.Increment();

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
                _log.Debug("Relay: {relay}. Received empty frame", _instance);
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
