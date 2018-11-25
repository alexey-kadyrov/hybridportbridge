using System.Net.Sockets;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public class LocalTcpDataChannelFactory : ILocalDataChannelFactory
    {
        private readonly MetricTags _tags;
        private readonly string _host;
        private readonly int _port;

        public LocalTcpDataChannelFactory(MetricTags tags, string host, int port)
        {
            _tags = tags;
            _host = host;
            _port = port;
        }

        public async Task<LocalDataChannel> Create(ILogger logger, LocalDataChannelMetrics metrics, ConnectionId connectionId)
        {
            var tcpClient = new TcpClient(AddressFamily.InterNetwork)
            {
                LingerState = { Enabled = true },
                NoDelay = true
            };

            await tcpClient.ConnectAsync(_host, _port);

            return new LocalTcpDataChannel(logger, metrics.Merge(_tags), connectionId.ToString(), tcpClient);
        }
    }
}
