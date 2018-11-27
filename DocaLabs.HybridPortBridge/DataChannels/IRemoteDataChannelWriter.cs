using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface IRemoteDataChannelWriter
    {
        Task WriteAsync(ConnectionId connectionId, byte[] buffer, int count);
    }
}
