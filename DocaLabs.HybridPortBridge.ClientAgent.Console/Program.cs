using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.ClientAgent.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ClientAgent.Console";

            var configuration = args.BuildConfiguration();

            MetricsRegistry.Build(configuration);

            var host = Start(configuration);

            Blocker.Block();

            host.Close();

            MetricsRegistry.Instance.Dispose();
        }

        private static PortBridgeClientForwarderHost Start(IConfiguration configuration)
        {
            var options = configuration.GetSection("PortBridge").Get<ClientAgentOptions>();

            var loggerFactory = LoggerFactory.Initialize(configuration);

            var host = new PortBridgeClientForwarderHost(loggerFactory, options);

            host.Open();

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
