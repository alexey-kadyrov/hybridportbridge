using System.Collections.Generic;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Hosting;
using DocaLabs.HybridPortBridge.Metrics;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public class ServiceForwarderHost : IForwarder
    {
        private readonly MetricsRegistry _metricsRegistry;
        private readonly IReadOnlyCollection<ServiceConnectionForwarder> _forwarders;

        private ServiceForwarderHost(MetricsRegistry metricsRegistry, IReadOnlyCollection<ServiceConnectionForwarder> forwarders)
        {
            _metricsRegistry = metricsRegistry;
            _forwarders = forwarders;
        }

        public static ConsoleAgentHost Build(string[] args)
        {
            var configuration = args.BuildConfiguration();

            var forwarderHost = Create(configuration)
                .GetAwaiter()
                .GetResult();

            return new ConsoleAgentHost(forwarderHost);
        }

        public static async Task<IForwarder> Create(IConfiguration configuration)
        {
            var options = configuration.GetSection("PortBridge").Get<ServiceAgentOptions>();

            var logger = LoggerBuilder.Initialize(configuration);

            var metricsRegistry = new MetricsRegistry(configuration);

            var forwarders = await BuildServiceForwarders(logger, metricsRegistry, options);

            return new ServiceForwarderHost(metricsRegistry, forwarders);
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

        private static async Task<IReadOnlyCollection<ServiceConnectionForwarder>> BuildServiceForwarders(ILogger logger, MetricsRegistry metricsRegistry, ServiceAgentOptions options)
        {
            var forwarders = new List<ServiceConnectionForwarder>();

            foreach (var entityPath in options.EntityPaths)
            {
                forwarders.Add(await ServiceConnectionForwarder.Create(logger, metricsRegistry, options.ServiceNamespace, entityPath));
            }

            return forwarders;
        }
    }
}