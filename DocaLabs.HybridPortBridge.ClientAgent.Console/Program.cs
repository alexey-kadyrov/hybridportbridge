namespace DocaLabs.HybridPortBridge.ClientAgent.Console
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ClientAgent.Console";

            var host = ClientForwarderHost.Build(args);

            host.Run();
        }
    }
}
