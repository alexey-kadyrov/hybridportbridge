using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Azure.Relay;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class RelayTunnelFactory : IDisposable
    {
        private readonly ILogger _log;
        private readonly TunnelMetrics _metrics;
        private readonly ConcurrentDictionary<object, RelayTunnel> _tunnels;

        public RelayTunnelFactory(ILogger logger, TunnelMetrics metrics)
        {
            _log = logger.ForContext(GetType());
            _metrics = metrics;
            _tunnels = new ConcurrentDictionary<object, RelayTunnel>();
        }

        public RelayTunnel Create(HybridConnectionStream stream, ILocalDataChannelFactory localFactory)
        {
            var tunnel = new RelayTunnel(_log, _metrics, stream, localFactory, OnTunnelCompleted);

            _tunnels[tunnel] = tunnel;

            return tunnel;
        }

        private Task OnTunnelCompleted(RelayTunnel tunnel)
        {
            if (_tunnels.TryRemove(tunnel, out _))
                tunnel.IgnoreException(x => x.Dispose());

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _tunnels.DisposeAndClear();
        }
    }
}
