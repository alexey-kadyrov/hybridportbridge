using System;
using Topshelf;

namespace DocaLabs.HybridPortBridge.ClientAgent.WindowsService
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var rc = HostFactory.Run(x => 
            {
                x.Service<ConsoleAgentHost>(sc =>
                {
                    sc.ConstructUsing(() => ClientForwarderHost.Build(args));
                    sc.WhenStarted(s => s.Start());
                    sc.WhenStopped(s => s.Stop());
                });

                x.SetDisplayName("DocaLabs.HybridPortBridge.ClientAgent.WindowsService");
                x.SetServiceName("DocaLabs.HybridPortBridge.ClientAgent.WindowsService");

                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(TimeSpan.FromSeconds(5));
                });
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}