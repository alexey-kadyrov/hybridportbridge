using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public interface ILocalDataChannelFactory
    {
        Task<LocalDataChannel> Create(ILogger logger, LocalDataChannelMetrics metrics, ConnectionId connectionId);
    }
}
