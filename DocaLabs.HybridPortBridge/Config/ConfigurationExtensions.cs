using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace DocaLabs.HybridPortBridge.Config
{
    public static class ConfigurationExtensions
    {
        public static IConfiguration BuildConfiguration(this string[] args)
        {
            return new ConfigurationBuilder()
                .BuildConfiguration(args)
                .Build();
        }

        public static IConfigurationBuilder BuildConfiguration(this IConfigurationBuilder builder, string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("Hosting:Environment");
            
            builder
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("/settings/appsettings.json", true)
                .AddJsonFile("/secrets/appsettings.json", true);

            if (!string.IsNullOrWhiteSpace(environmentName))
                builder.AddJsonFile($"appsettings.{environmentName}.json", true);

            builder.AddEnvironmentVariables();

            if (args != null && args.Any())
                builder.AddCommandLine(args);

            return builder;
        }
    }
}
