using System;
using System.Diagnostics;
using System.IO;
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

            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);

            builder
                .SetBasePath(pathToContentRoot)
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
