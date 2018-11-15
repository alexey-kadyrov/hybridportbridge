using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface IRelayDataChannelReader
    {
        Task<Frame> ReadAsync();
    }
}
