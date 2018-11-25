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
        private readonly MetricsRegistry _metrics;
        private readonly IReadOnlyCollection<IForwarder> _forwarders;

        private ClientForwarderHost(MetricsRegistry metrics, IReadOnlyCollection<IForwarder> forwarders)
        {
            _metrics = metrics;
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

            var logger = LoggerBuilder.Initialize(configuration);

            var metricsRegistry = MetricsRegistry.CreateRoot(configuration);

            var forwarders = BuildPortForwarders(logger, metricsRegistry, options);

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

            _metrics.Dispose();
        }

        private static IReadOnlyCollection<IForwarder> BuildPortForwarders(ILogger logger, MetricsRegistry metrics, ClientAgentOptions options)
        {
            var forwarders = new List<IForwarder>();

            foreach (var mapping in options.PortMappings)
            {
                if (!int.TryParse(mapping.Key, out var port))
                    throw new ConfigurationErrorException($"Invalid {mapping.Key} port number");

                forwarders.Add(new ClientTcpForwarder(logger, metrics, options.ServiceNamespace, port, mapping.Value));
            }

            return forwarders;
        }
    }
}