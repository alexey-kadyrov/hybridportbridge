namespace DocaLabs.HybridPortBridge.ClientAgent.Console
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ClientAgent.Console";

            var host = ClientForwarderHost.Build(args);

            host.Run();
        }
    }
}
