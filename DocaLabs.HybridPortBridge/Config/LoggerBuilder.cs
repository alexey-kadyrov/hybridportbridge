using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge.Config
{
    public static class LoggerBuilder
    {
        public static ILogger Initialize(IConfiguration configuration)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
        }
    }
}
