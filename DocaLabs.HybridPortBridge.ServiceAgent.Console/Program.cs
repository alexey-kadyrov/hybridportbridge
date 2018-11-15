using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.ServiceAgent.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ServiceAgent.Console";

            var configuration = args.BuildConfiguration();

            MetricsRegistry.Build(configuration);

            var host = Start(configuration);

            Blocker.Block();

            host.Stop();

            MetricsRegistry.Instance.Dispose();
        }

        private static PortBridgeServiceForwarderHost Start(IConfiguration configuration)
        {
            var options = configuration.GetSection("PortBridge").Get<ServiceAgentOptions>();

            var loggerFactory = LoggerBuilder.Initialize(configuration);

            var host = new PortBridgeServiceForwarderHost(loggerFactory, options);

            host.Start();

            return host;
        }

        [ExcludeFromCodeCoverage]
        public static class Blocker
        {
            private static readonly AutoResetEvent Closing = new AutoResetEvent(false);

            public static void Block()
            {
                System.Console.CancelKeyPress += CancelKeyPress;
                Closing.WaitOne();
            }

            public static void Release()
            {
                Closing.Set();
            }

            private static void CancelKeyPress(object sender, ConsoleCancelEventArgs args)
            {
                Release();
            }
        }
    }
}
