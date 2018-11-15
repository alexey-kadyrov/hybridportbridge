using System.Collections.Generic;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent.Console
{
    internal class PortBridgeServiceForwarderHost
    {
        public PortBridgeServiceForwarderHost(ILogger loggerFactory, ServiceAgentOptions options)
        {
            Forwarders = BuildServiceForwarders(loggerFactory, options);
        }

        public ICollection<ServiceConnectionForwarder> Forwarders { get; }

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

        private static ICollection<ServiceConnectionForwarder> BuildServiceForwarders(ILogger loggerFactory, ServiceAgentOptions options)
        {
            var forwarders = new List<ServiceConnectionForwarder>();

            for (var i = 0; i < options.EntityPaths.Count; ++i)
            {
                forwarders.Add(new ServiceConnectionForwarder(loggerFactory, i, options.ServiceNamespace, options.EntityPaths[i]));
            }

            return forwarders;
        }
    }
}