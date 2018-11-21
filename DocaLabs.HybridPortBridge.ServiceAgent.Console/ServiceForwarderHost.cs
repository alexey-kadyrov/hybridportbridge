using System.Collections.Generic;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent.Console
{
    public class ServiceForwarderHost : IForwarder
    {
        public IReadOnlyCollection<ServiceConnectionForwarder> Forwarders { get; }

        private ServiceForwarderHost(IReadOnlyCollection<ServiceConnectionForwarder> forwarders)
        {
            Forwarders = forwarders;
        }

        public static AgentHost Build(string[] args)
        {
            var configuration = args.BuildConfiguration();

            var forwarderHost = Create(configuration)
                .GetAwaiter()
                .GetResult();

            return new AgentHost(forwarderHost);
        }

        private static async Task<ServiceForwarderHost> Create(IConfiguration configuration)
        {
            var options = configuration.GetSection("PortBridge").Get<ServiceAgentOptions>();

            var logger = LoggerBuilder.Initialize(configuration);

            MetricsRegistry.Build(configuration);

            var forwarders = await BuildServiceForwarders(logger, options);

            return new ServiceForwarderHost(forwarders);
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