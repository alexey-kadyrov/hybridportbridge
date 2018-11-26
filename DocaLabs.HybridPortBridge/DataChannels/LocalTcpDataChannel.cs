using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class LocalTcpDataChannel : LocalDataChannel
    {
        private readonly ILogger _log;
        private readonly string _instance;
        private readonly LocalDataChannelMetrics _metrics;
        private readonly TcpClient _endpoint;
        private readonly Stream _stream;
        private readonly byte[] _buffer;

        public LocalTcpDataChannel(ILogger log, LocalDataChannelMetrics metrics, string instance, TcpClient endpoint)
        {
            _log = log.ForContext(GetType());
            _metrics = metrics;
            _endpoint = endpoint;
            _instance = instance;
            _stream = _endpoint.GetStream();
            _buffer = new byte[BufferSize];
            _log.Debug("Local: {localInstance} is initialized", _instance);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
                _endpoint.IgnoreException(x => x.Dispose());
        }

        public override async Task WriteAsync(Frame frame)
        {
            try
            {
                await _stream.WriteAsync(frame.Buffer, 0, frame.Size);

                _log.Verbose("ConnectId: {connectionId}. Written {frameSize}", frame.ConnectionId, frame.Size);

                _metrics.FrameWritten(frame.Size);
            }
            catch (Exception e)
            {
                _metrics.Failed();

                _log.Error(e, "ConnectId: {connectionId}. Failed to write", frame.ConnectionId);

                throw;
            }
        }

        public override async Task<(int BytesRead, byte[] Data)> ReadAsync()
        {
            try
            {
                var count = await _stream.ReadAsync(_buffer, RemoteRelayDataChannel.PreambleByteSize, BufferSize - RemoteRelayDataChannel.PreambleByteSize);

                _metrics.FrameRead(count);

                return (count, _buffer);
            }
            catch (Exception e)
            {
                _metrics.Failed();

                _log.Error(e, "Local: {localInstance}. Failed to read", _instance);

                throw;
            }
        }
    }
}
