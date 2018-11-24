using System.Collections.Generic;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Hosting;
using DocaLabs.HybridPortBridge.Metrics;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    public class ClientForwarderHost : IForwarder
    {
        private readonly MetricsRegistry _metricsRegistry;
        private readonly IReadOnlyCollection<IForwarder> _forwarders;

        private ClientForwarderHost(MetricsRegistry metricsRegistry, IReadOnlyCollection<IForwarder> forwarders)
        {
            _metricsRegistry = metricsRegistry;
            _forwarders = forwarders;
        }

        public static ConsoleAgentHost Build(string[] args)
        {
            var configuration = args.BuildConfiguration();

            return new ConsoleAgentHost(Create(configuration));
        }

        public static ClientForwarderHost Create(IConfiguration configuration)
        {
            var options = configuration.GetSection("PortBridge").Get<ClientAgentOptions>();

            var loggerFactory = LoggerBuilder.Initialize(configuration);

            var metricsRegistry = MetricsRegistry.CreateRoot(configuration);

            var forwarders = BuildPortForwarders(loggerFactory, metricsRegistry, options);

            return new ClientForwarderHost(metricsRegistry, forwarders);
        }

        public void Start()
        {
            foreach (var forwarder in _forwarders)
            {
                forwarder.Start();
            }
        }

        public void Stop()
        {
            foreach (var forwarder in _forwarders)
            {
                forwarder.Stop();
            }

            _metricsRegistry.Dispose();
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