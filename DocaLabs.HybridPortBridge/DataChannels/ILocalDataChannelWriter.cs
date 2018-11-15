using System;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface ILocalDataChannelWriter : IDisposable
    {
        Task WriteAsync(Frame frame);
    }
}