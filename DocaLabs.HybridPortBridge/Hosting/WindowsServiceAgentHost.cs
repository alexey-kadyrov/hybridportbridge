using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DocaLabs.HybridPortBridge.Hosting
{
    public class WindowsServiceAgentHost : IHostedService
    {
        private readonly IForwarder _forwarder;

        public WindowsServiceAgentHost(IForwarder forwarder)
        {
            _forwarder = forwarder;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _forwarder.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _forwarder.Stop();
            return Task.CompletedTask;
        }
    }
}