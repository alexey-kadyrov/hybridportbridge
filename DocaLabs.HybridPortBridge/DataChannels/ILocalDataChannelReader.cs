using System;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.DataChannels
{
    public interface ILocalDataChannelReader : IDisposable
    {
        Task<(int BytesRead, byte[] Data)> ReadAsync();
    }
}