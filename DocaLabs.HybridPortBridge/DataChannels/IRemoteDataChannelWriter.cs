using System;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface IRemoteDataChannelWriter : IDisposable
    {
        Task WriteAsync(ConnectionId connectionId, byte[] buffer, int count);
    }
}
