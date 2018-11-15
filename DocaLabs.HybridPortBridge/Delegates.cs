using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge
{
    public delegate Task<int> BufferReadAsync(byte[] buffer, int offset, int count);

    public delegate Task BufferWriteAsync(byte[] buffer, int offset, int count);
}