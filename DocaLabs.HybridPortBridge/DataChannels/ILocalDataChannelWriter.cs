using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface ILocalDataChannelWriter
    {
        Task WriteAsync(Frame frame);
    }
}