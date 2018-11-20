namespace DocaLabs.HybridPortBridge.ClientAgent.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ClientAgent.Console";

            var host = PortBridgeClientForwarderHost.Configure(args);

            host.Run();
        }
    }
}
