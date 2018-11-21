using System.Collections.Generic;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent.Console
{
    public class ClientForwarderHost : IForwarder
    {
        public ICollection<IForwarder> Forwarders { get; }

        private ClientForwarderHost(ICollection<IForwarder> forwarders)
        {
            Forwarders = forwarders;
        }

        public static AgentHost Build(string[] args)
        {
            var configuration = args.BuildConfiguration();

            return new AgentHost(Create(configuration));
        }

        private static ClientForwarderHost Create(IConfiguration configuration)
        {
            var options = configuration.GetSection("PortBridge").Get<ClientAgentOptions>();

            var loggerFactory = LoggerBuilder.Initialize(configuration);

            MetricsRegistry.Build(configuration);

            return new ClientForwarderHost(BuildPortForwarders(loggerFactory, options));
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

            MetricsRegistry.Factory.Dispose();
        }

        private static ICollection<IForwarder> BuildPortForwarders(ILogger loggerFactory, ClientAgentOptions options)
        {
            var forwarders = new List<IForwarder>();

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