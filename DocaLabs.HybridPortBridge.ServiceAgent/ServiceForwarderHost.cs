using System.Collections.Generic;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Metrics;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public class ServiceForwarderHost : IForwarder
    {
        public const string DefaultConfigurationSectionName = "PortBridge";
        
        private readonly MetricsRegistry _metrics;
        private readonly IReadOnlyCollection<ServiceForwarder> _forwarders;

        private ServiceForwarderHost(MetricsRegistry metrics, IReadOnlyCollection<ServiceForwarder> forwarders)
        {
            _metrics = metrics;
            _forwarders = forwarders;
        }

        public static ConsoleAgentHost Build(string[] args, string sectionName = null)
        {
            var configuration = args.BuildConfiguration();

            var forwarderHost = Create(configuration, sectionName)
                .GetAwaiter()
                .GetResult();

            return new ConsoleAgentHost(forwarderHost);
        }

        public static async Task<IForwarder> Create(IConfiguration configuration, string sectionName = null)
        {
            var options = configuration.GetSection(sectionName ?? DefaultConfigurationSectionName).Get<ServiceAgentOptions>();

            var logger = LoggerBuilder.Initialize(configuration);

            var metricsRegistry = MetricsRegistry.CreateRoot(configuration);

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

            _metrics.Dispose();
        }

        private static async Task<IReadOnlyCollection<ServiceForwarder>> BuildServiceForwarders(ILogger logger, MetricsRegistry metrics, ServiceAgentOptions options)
        {
            var forwarders = new List<ServiceForwarder>();

            foreach (var entityPath in options.EntityPaths)
            {
                forwarders.Add(await ServiceForwarder.Create(logger, metrics, options.ServiceNamespace, entityPath));
            }

            return forwarders;
        }
    }
}