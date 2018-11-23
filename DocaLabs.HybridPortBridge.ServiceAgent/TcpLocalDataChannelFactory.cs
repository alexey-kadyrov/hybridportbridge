using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public class TcpLocalDataChannelFactory : ILocalDataChannelFactory
    {
        private readonly string _host;
        private readonly int _port;

        public TcpLocalDataChannelFactory(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task<LocalDataChannel> Create(ILogger logger, MetricsRegistry metrics, ConnectionId connectionId)
        {
            var tcpClient = new TcpClient(AddressFamily.InterNetwork)
            {
                LingerState = { Enabled = true },
                NoDelay = true
            };

            await tcpClient.ConnectAsync(_host, _port);

            return new LocalTcpDataChannel(logger, metricsRegistry, connectionId.ToString(), metricTags, tcpClient);
        }
    }
}
