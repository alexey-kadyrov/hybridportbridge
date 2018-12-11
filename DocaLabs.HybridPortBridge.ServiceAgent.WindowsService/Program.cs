using System;
using Topshelf;

namespace DocaLabs.HybridPortBridge.ServiceAgent.WindowsService
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var rc = HostFactory.Run(x =>
            {
                x.Service<ConsoleAgentHost>(sc =>
                {
                    sc.ConstructUsing(() => ServiceForwarderHost.Build(args));
                    sc.WhenStarted(s => s.Start());
                    sc.WhenStopped(s => s.Stop());
                });

                x.SetDisplayName("DocaLabs.HybridPortBridge.ServiceAgent.WindowsService");
                x.SetServiceName("DocaLabs.HybridPortBridge.ServiceAgent.WindowsService");

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