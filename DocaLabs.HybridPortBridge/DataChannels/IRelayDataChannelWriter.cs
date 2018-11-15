using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface IRelayDataChannelWriter
    {
        Task WriteAsync(ConnectionId connectionId, byte[] buffer, int count);
    }
}
