namespace DocaLabs.HybridPortBridge.ServiceAgent.Console
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            System.Console.Title = "DocaLabs.HybridPortBridge.ServiceAgent.Console";

            var host = ServiceForwarderHost.Build(args);

            host.Run();
        }
    }
}
