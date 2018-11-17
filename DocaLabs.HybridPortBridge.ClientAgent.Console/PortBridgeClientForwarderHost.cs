using System.Collections.Generic;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent.Console
{
    internal class PortBridgeClientForwarderHost
    {
        public PortBridgeClientForwarderHost(ILogger loggerFactory, ClientAgentOptions options)
        {
            Forwarders = BuildPortForwarders(loggerFactory, options);
        }

        public ICollection<IConnectionForwarder> Forwarders { get; }

        public void Open()
        {
            foreach (var forwarder in Forwarders)
            {
                forwarder.Start();
            }
        }

        public void Close()
        {
            foreach (var forwarder in Forwarders)
            {
                forwarder.Stop();
            }
        }

        private static ICollection<IConnectionForwarder> BuildPortForwarders(ILogger loggerFactory, ClientAgentOptions options)
        {
            var forwarders = new List<IConnectionForwarder>();

            foreach (var mapping in options.PortMappings)
            {
                if (!int.TryParse(mapping.Key, out var port))
                    throw new ConfigurationErrorException($"Invalid {mapping.Key} port number");

                forwarders.Add(new TcpClientConnectionForwarder(loggerFactory, options.ServiceNamespace, port, mapping.Value));
            }

            return forwarders;
        }

    }
}