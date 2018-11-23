using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.DataChannels;
using DocaLabs.HybridPortBridge.Metrics;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public interface ILocalDataChannelFactory
    {
        Task<LocalDataChannel> Create(ILogger logger, MetricsRegistry metricsRegistry, MetricTags metricTags, ConnectionId connectionId);
    }
}
