using System.Collections.Generic;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent.Console
{
    internal class PortBridgeServiceForwarderHost
    {
        public IReadOnlyCollection<ServiceConnectionForwarder> Forwarders { get; }

        private PortBridgeServiceForwarderHost(IReadOnlyCollection<ServiceConnectionForwarder> forwarders)
        {
            Forwarders = forwarders;
        }
        public static async Task<PortBridgeServiceForwarderHost> Create(IConfiguration configuration)
        {
            var options = configuration.GetSection("PortBridge").Get<ServiceAgentOptions>();

            var logger = LoggerBuilder.Initialize(configuration);

            var forwarders = await BuildServiceForwarders(logger, options);

            return new PortBridgeServiceForwarderHost(forwarders);
        }

        public void Start()
        {
            foreach (var forwarder in Forwarders)
            {
                forwarder.Start();
            }
        }

        public void Stop()
        {
            foreach (var forwarder in Forwarders)
            {
                forwarder.Stop();
            }
        }

        private static async Task<IReadOnlyCollection<ServiceConnectionForwarder>> BuildServiceForwarders(ILogger logger, ServiceAgentOptions options)
        {
            var forwarders = new List<ServiceConnectionForwarder>();

            foreach (var entityPath in options.EntityPaths)
            {
                forwarders.Add(await ServiceConnectionForwarder.Create(logger, options.ServiceNamespace, entityPath));
            }

            return forwarders;
        }
    }
}