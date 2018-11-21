using System;
using System.Collections.Concurrent;
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
            FrameQueue queue;

            if (_correlateLocalWriter == null)
            {
                if (!_queues.TryGetValue(frame.ConnectionId, out queue))
                {
                    _log.Warning("CorrelationId: {correlationId}. There is no local writer for the frame");
                    return;
                }
            }
            else
            {
                queue = _queues.GetOrAdd(frame.ConnectionId,
                    k => new FrameQueue(_log, _correlateLocalWriter(frame.ConnectionId).GetAwaiter().GetResult(), CompleteLocalWriter));
            }

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

        public void Clear()
        {
            _queues.DisposeAndClear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
