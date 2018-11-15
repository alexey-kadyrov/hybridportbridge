using System;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface IRemoteDataChannelReader : IDisposable
    {
        Task<Frame> ReadAsync();
    }
}
