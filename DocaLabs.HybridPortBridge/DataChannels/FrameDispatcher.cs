using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public delegate Task<ILocalDataChannelWriter> LocalWriterFactory(ConnectionId id);
    public delegate void CompleteLocalWriter(ConnectionId id);

    public sealed class FrameDispatcher
    {
        private readonly ILogger _log;
        private readonly LocalWriterFactory _localWriterFactory;
        private readonly ConcurrentDictionary<ConnectionId, FrameQueue> _queues;

        public FrameDispatcher(ILogger logger, LocalWriterFactory localWriterFactory)
            : this(logger)
        {
            _localWriterFactory = localWriterFactory;
        }

        public FrameDispatcher(ILogger logger)
        {
            _log = logger.ForContext(GetType());
            _queues = new ConcurrentDictionary<ConnectionId, FrameQueue>();
        }

        public void AddQueue(ConnectionId connectionId, ILocalDataChannelWriter writer)
        {
            _queues.TryAdd(connectionId, new FrameQueue(_log, writer, CompleteLocalWriter));
        }

        public void RemoveQueue(ConnectionId connectionId)
        {
            CompleteLocalWriter(connectionId);
        }

        public void DispatchFrame(Frame frame)
        {
            _log.Verbose("ConnectionId {connectionId}. Dispatching frame, size {frameSize}", frame.ConnectionId, frame.Size);

            var queue = _queues.GetOrAdd(frame.ConnectionId, k =>
            {
                _log.Verbose("ConnectionId {connectionId}. Requesting local writer, frame size {frameSize}", frame.ConnectionId, frame.Size);
                return new FrameQueue(_log, _localWriterFactory(frame.ConnectionId).GetAwaiter().GetResult(), CompleteLocalWriter);
            });

            queue.ProcessAsync(frame);
        }

        private void CompleteLocalWriter(ConnectionId connectionId)
        {
            if (_queues.TryRemove(connectionId, out var queue))
            {
                _log.Verbose("ConnectionId: {connectionId}. Shutting down local writer.", connectionId);

                queue.Dispose();
            }
            else
            {
                _log.Debug("ConnectionId: {connectionId}. No local writer was found to shutdown.", connectionId);
            }
        }

        private void Drain()
        {
            try
            {
                _log.Verbose("Frame dispatcher is draining.");

                var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                var tasks = _queues.Select(q => q.Value.WaitToDrain(tokenSource.Token));
                
                Task.WhenAll(tasks).GetAwaiter().GetResult();
                
                _queues.Clear();
            }
            catch
            {
                // intentional
            }
        }

        public void Clear()
        {
            Drain();
            
            _queues.DisposeAndClear();

            _log.Verbose("Frame dispatcher cleared.");
        }
    }
}
