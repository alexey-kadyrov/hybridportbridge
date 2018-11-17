using System;
using System.Threading;
using System.Threading.Tasks;

namespace DocaLabs.HybridPortBridge.ServiceAgent.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ServiceAgent.Console";

            MainAsync(args)
                .GetAwaiter()
                .GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var configuration = args.BuildConfiguration();

            MetricsRegistry.Build(configuration);

            var host = await PortBridgeServiceForwarderHost.Create(configuration);

            host.Start();

            Blocker.Block();

            host.Stop();

            MetricsRegistry.Factory.Dispose();
        }

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
