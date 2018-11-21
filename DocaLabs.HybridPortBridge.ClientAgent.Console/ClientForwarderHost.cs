using System.Collections.Generic;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent.Console
{
    public class ClientForwarderHost : IForwarder
    {
        public MetricsRegistry MetricsRegistry { get; }

        public IReadOnlyCollection<IForwarder> Forwarders { get; }

        private ClientForwarderHost(MetricsRegistry metricsRegistry, IReadOnlyCollection<IForwarder> forwarders)
        {
            MetricsRegistry = metricsRegistry;
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

            var metricsRegistry = new MetricsRegistry(configuration);

            var forwarders = BuildPortForwarders(loggerFactory, metricsRegistry, options);

            return new ClientForwarderHost(metricsRegistry, forwarders);
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

            MetricsRegistry.Dispose();
        }

        private static IReadOnlyCollection<IForwarder> BuildPortForwarders(ILogger loggerFactory, MetricsRegistry metricsRegistry, ClientAgentOptions options)
        {
            var forwarders = new List<IForwarder>();

            foreach (var mapping in options.PortMappings)
            {
                if (!int.TryParse(mapping.Key, out var port))
                    throw new ConfigurationErrorException($"Invalid {mapping.Key} port number");

                forwarders.Add(new TcpClientConnectionForwarder(loggerFactory, metricsRegistry, options.ServiceNamespace, port, mapping.Value));
            }

            return forwarders;
        }
    }
}