namespace DocaLabs.HybridPortBridge.ServiceAgent.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ServiceAgent.Console";

            var host = PortBridgeServiceForwarderHost.Configure(args);

            host.Run();
        }
    }
}
