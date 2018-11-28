using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public sealed class FrameQueue : IDisposable
    {
        private readonly ILogger _log;
        private readonly CompleteLocalWriter _completeLocalWriter;
        private readonly ConcurrentQueue<Frame> _frames;
        private readonly SemaphoreSlim _locker;
        private readonly ILocalDataChannelWriter _writer;

        public FrameQueue(ILogger logger, ILocalDataChannelWriter writer, CompleteLocalWriter completeLocalWriter)
        {
            _log = logger.ForContext(GetType());
            _writer = writer;
            _completeLocalWriter = completeLocalWriter;
            _locker = new SemaphoreSlim(1, 1);
            _frames = new ConcurrentQueue<Frame>();
        }

        public Task ProcessAsync(Frame frame)
        {
            _log.Verbose("ConnectId: {connectionId}. Enqueue frame {frameSize}", frame.ConnectionId, frame.Size);

            _frames.Enqueue(frame);

            return DequeueAsync();
        }

        private async Task DequeueAsync()
        {
            await _locker.WaitAsync();

            try
            {
                while (_frames.TryDequeue(out var frame))
                {
                    var completeWriteBack = frame.Size == 0;

                    _log.Verbose("ConnectId: {connectionId}. Dequeue frame {frameSize}", frame.ConnectionId, frame.Size);

                    try
                    {
                        await _writer.WriteAsync(frame);
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
                        _completeLocalWriter(frame.ConnectionId);
                }
            }
            finally
            {
                _locker.Release();
            }
        }

        public Task WaitToDrain(CancellationToken token)
        {
            while (!_frames.IsEmpty)
            {
                if(token.IsCancellationRequested)
                    break;
            }
            
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
            _locker.IgnoreException(x => x.Dispose());
        }
    }
}