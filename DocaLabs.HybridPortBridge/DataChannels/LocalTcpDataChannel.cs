using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Meter;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class LocalTcpDataChannel : LocalDataChannel
    {
        private static readonly MeterOptions FailuresOptions = new MeterOptions
        {
            Name = "Failure (Local)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions FrameReadOptions = new MeterOptions
        {
            Name = "Frame Read (Local)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions FrameWrittenOptions = new MeterOptions
        {
            Name = "Frame Written (Local)",
            MeasurementUnit = Unit.Items
        };

        private static readonly MeterOptions BytesReadOptions = new MeterOptions
        {
            Name = "Bytes Read (Local)",
            MeasurementUnit = Unit.Bytes
        };

        private static readonly MeterOptions BytesWrittenOptions = new MeterOptions
        {
            Name = "Bytes Written (Local)",
            MeasurementUnit = Unit.Bytes
        };

        private readonly ILogger _log;
        private readonly MeterMetric _failures;
        private readonly MeterMetric _frameRead;
        private readonly MeterMetric _frameWritten;
        private readonly MeterMetric _bytesRead;
        private readonly MeterMetric _bytesWritten;
        private readonly string _instance;
        private readonly TcpClient _endpoint;
        private readonly Stream _stream;
        private readonly byte[] _buffer;

        public LocalTcpDataChannel(ILogger log, MetricsFactory metrics, string instance, MetricTags tags, TcpClient endpoint)
        {
            _log = log.ForContext(GetType());
            _endpoint = endpoint;
            _instance = instance;
            _stream = _endpoint.GetStream();
            _buffer = new byte[BufferSize];

            _failures = metrics.MakeMeter(FailuresOptions, tags);
            _frameRead = metrics.MakeMeter(FrameReadOptions, tags);
            _frameWritten = metrics.MakeMeter(FrameWrittenOptions, tags);
            _bytesRead = metrics.MakeMeter(BytesReadOptions, tags);
            _bytesWritten = metrics.MakeMeter(BytesWrittenOptions, tags);

            _log.Debug("Local: {local} is initialized", _instance);
        }

        protected override void Dispose(bool disposing)
        {
            _endpoint.IgnoreException(x => x.Dispose());
        }

        public override async Task WriteAsync(Frame frame)
        {
            try
            {
                await _stream.WriteAsync(frame.Buffer, 0, frame.Size);

                _frameWritten.Increment();

                _bytesWritten.Increment(frame.Size);
            }
            catch (Exception e)
            {
                _failures.Increment();

                _log.Error(e, "ConnectId: {connectionId}. Failed to write", frame.ConnectionId);

                throw;
            }
        }

        public override async Task<(int BytesRead, byte[] Data)> ReadAsync()
        {
            try
            {
                var count = await _stream.ReadAsync(_buffer, RemoteRelayDataChannel.PreambleByteSize, BufferSize - RemoteRelayDataChannel.PreambleByteSize);

                _frameRead.Increment();

                _bytesRead.Increment(count);

                return (count, _buffer);
            }
            catch (Exception e)
            {
                _failures.Increment();

                _log.Error(e, "Local: {local}. Failed to read", _instance);

                throw;
            }
        }
    }
}
