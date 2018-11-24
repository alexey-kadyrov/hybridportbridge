using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public delegate Task<ILocalDataChannelWriter> CorrelateLocalWriter(ConnectionId id);
    public delegate void CompleteLocalWriter(ConnectionId id);

    public sealed class FrameDispatcher : IDisposable
    {
        private readonly ILogger _log;
        private readonly CorrelateLocalWriter _correlateLocalWriter;
        private readonly ConcurrentDictionary<ConnectionId, FrameQueue> _queues;
        private bool _stopped;

        public FrameDispatcher(ILogger logger, CorrelateLocalWriter correlateLocalWriter)
            : this(logger)
        {
            _correlateLocalWriter = correlateLocalWriter;
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
            if(_stopped)
                return;
            
            var queue = _queues.GetOrAdd(frame.ConnectionId,
                k => new FrameQueue(_log, _correlateLocalWriter(frame.ConnectionId).GetAwaiter().GetResult(), CompleteLocalWriter));

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

        public void Drain()
        {
            try
            {
                if(_stopped)
                    return;
                
                _stopped = true;

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

        public void Dispose()
        {
            Drain();
            
            _queues.DisposeAndClear();
        }
    }
}
