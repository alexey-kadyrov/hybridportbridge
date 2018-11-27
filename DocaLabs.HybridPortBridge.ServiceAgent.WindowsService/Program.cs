using System.Diagnostics;
using System.Linq;
using DocaLabs.HybridPortBridge.Hosting;

namespace DocaLabs.HybridPortBridge.ServiceAgent.WindowsService
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ServiceAgent.WindowsService";

            var asService = !(Debugger.IsAttached || args.Contains("--console"));

            args = args
                .Where(x => x != "--console")
                .ToArray();
            
            if (asService)
            {
                ConfigureWindowsServiceHostBuilder.Configure(args, c => ServiceForwarderHost.Create(c).GetAwaiter().GetResult()).RunAsServiceAsync();
            }
            else
            {
                ServiceForwarderHost.Build(args).Run();
            }
        }
    }
}