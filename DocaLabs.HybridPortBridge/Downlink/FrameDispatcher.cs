using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Serilog;

namespace DocaLabs.HybridPortBridge.Downlink
{
    public delegate Task<EndpointWriter> CorrelateEndpointWriter(ConnectionId id);
    public delegate void CompleteEndpointWriter(ConnectionId id);

    public sealed class FrameDispatcher : IDisposable
    {
        private readonly ILogger _log;
        private readonly CorrelateEndpointWriter _correlateEndpointWriter;
        private readonly ConcurrentDictionary<ConnectionId, FrameQueue> _queues;

        public FrameDispatcher(ILogger loggerFactory, CorrelateEndpointWriter correlateEndpointWriter)
            : this(loggerFactory)
        {
            _correlateEndpointWriter = correlateEndpointWriter;
        }

        public FrameDispatcher(ILogger loggerFactory)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
            _queues = new ConcurrentDictionary<ConnectionId, FrameQueue>();
        }

        public void AddQueue(ConnectionId connectionId, EndpointWriter writer)
        {
            _queues.TryAdd(connectionId, new FrameQueue(_log, writer, CompleteEndpointWriter));
        }

        public void RemoveQueue(ConnectionId connectionId)
        {
            CompleteEndpointWriter(connectionId);
        }

        public Task DispatchFrame(Frame frame)
        {
            FrameQueue queue;

            if (_correlateEndpointWriter == null)
            {
                if (!_queues.TryGetValue(frame.ConnectionId, out queue))
                {
                    _log.Warning("CorrelationId: {correlationId}. There is no frame writer for the frame");
                    return Task.CompletedTask;
                }
            }
            else
                queue = _queues.GetOrAdd(frame.ConnectionId, k => 
                    new FrameQueue(_log, _correlateEndpointWriter(frame.ConnectionId).GetAwaiter().GetResult(), CompleteEndpointWriter));

            return queue.ProcessAsync(frame);
        }

        private void CompleteEndpointWriter(ConnectionId connectionId)
        {
            if (_queues.TryRemove(connectionId, out var queue))
            {
                _log.Verbose("ConnectionId: {connectionId}. Shutting down frame writer.", connectionId);

                queue.Dispose();
            }
            else
            {
                _log.Debug("ConnectionId: {connectionId}. FNo frame writer was found to shutdown.", connectionId);
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
