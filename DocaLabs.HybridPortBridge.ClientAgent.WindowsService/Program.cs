using System.Diagnostics;
using System.Linq;
using DocaLabs.HybridPortBridge.Hosting;

namespace DocaLabs.HybridPortBridge.ClientAgent.WindowsService
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ClientAgent.WindowsService";

            var asService = !(Debugger.IsAttached || args.Contains("--console"));

            args = args
                .Where(x => x != "--console")
                .ToArray();
            
            if (asService)
            {
                ConfigureWindowsServiceHostBuilder.Configure(args, ClientForwarderHost.Create).RunAsServiceAsync();
            }
            else
            {
                ClientForwarderHost.Build(args).Run();
            }
        }
    }
}