using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class LocalTcpDataChannel : LocalDataChannel
    {
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

        public LocalTcpDataChannel(ILogger log, MetricsRegistry metrics, string instance, MetricTags tags, TcpClient endpoint)
        {
            _log = log.ForContext(GetType());
            _endpoint = endpoint;
            _instance = instance;
            _stream = _endpoint.GetStream();
            _buffer = new byte[BufferSize];

            _failures = metrics.MakeMeter(MetricsRegistry.LocalFailuresOptions, tags);
            _frameRead = metrics.MakeMeter(MetricsRegistry.LocalFrameReadOptions, tags);
            _frameWritten = metrics.MakeMeter(MetricsRegistry.LocalFrameWrittenOptions, tags);
            _bytesRead = metrics.MakeMeter(MetricsRegistry.LocalBytesReadOptions, tags);
            _bytesWritten = metrics.MakeMeter(MetricsRegistry.LocalBytesWrittenOptions, tags);

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
