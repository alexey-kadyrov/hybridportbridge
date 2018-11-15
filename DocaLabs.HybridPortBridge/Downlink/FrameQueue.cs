using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DocaLabs.HybridPortBridge.Downlink
{
    public sealed class FrameQueue : IDisposable
    {
        private readonly ILogger _log;
        private readonly CompleteEndpointWriter _completeEndpointWriter;
        private readonly ConcurrentQueue<Frame> _frames;
        private readonly SemaphoreSlim _locker;
        private readonly EndpointWriter _writer;

        public FrameQueue(ILogger loggerFactory, EndpointWriter writer, CompleteEndpointWriter completeEndpointWriter)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _writer = writer;
            _completeEndpointWriter = completeEndpointWriter;
            _locker = new SemaphoreSlim(1, 1);
            _frames = new ConcurrentQueue<Frame>();
        }

        public Task ProcessAsync(Frame newFrame)
        {
            _frames.Enqueue(newFrame);

            return DequeueAsync();
        }

        private async Task DequeueAsync()
        {
            await _locker.WaitAsync();

            try
            {
                while (_frames.TryDequeue(out var frame))
                {
                    var completeWriteBack = frame.FrameSize == 0;

                    try
                    {
                        await frame.WriteAsync(_writer);
                    }
                    catch (Exception e)
                    {
                        var se = e.Find<SocketException>(x => x.ErrorCode.In(10004, 10054));
                        if (se != null)
                            _log.Information("ConnectionId: {connectionId}. Socket canceled with code {errorCode} during pending read: {errorMessage}", frame.ConnectionId, se.ErrorCode, se.Message);
                        else
                            _log.Error(e, "ConnectionId: {connectionId}. Unable to write to multiplexed connection", frame.ConnectionId);

                        completeWriteBack = true;
                    }

                    if (completeWriteBack)
                        _completeEndpointWriter(frame.ConnectionId);
                }
            }
            finally
            {
                _locker.Release();
            }
        }

        public void Dispose()
        {
            _locker.IgnoreException(x => x.Dispose());
            _writer.IgnoreException(x => x.Dispose());
        }
    }
}