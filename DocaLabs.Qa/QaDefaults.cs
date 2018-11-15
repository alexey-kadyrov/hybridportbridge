using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.Qa
{
    public static class QaDefaults
    {
        public static object SerilogConsoleLoggingConfiguration { get; } = new
        {
            Logging = new
            {
                LogLevel = new
                {
                    Default = "Debug",
                    System = "Information",
                    Microsoft = "Warning"
                }
            },
            Serilog = new
            {
                Using = new[] { "Serilog.Sinks.Console" },
                MinimumLevel = new
                {
                    Default = "Verbose",
                    Override = new
                    {
                        Microsoft = "Warning"
                    }
                },
                WriteTo = new[]
                {
                    new
                    {
                        Name = "Console",
                        Args = new
                        {
                            outputTemplate =
                                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] <{SourceContext}> {Message}{NewLine}{Exception}"
                        }
                    }
                },
                Properties = new
                {
                    Application = "Service Under Test"
                }
            }
        };

        public static string[] GetSerilogConfigurationArgs(string service, string path)
        {
            var o = new
            {
                Logging = new
                {
                    LogLevel = new
                    {
                        Default = "Debug",
                        System = "Information",
                        Microsoft = "Warning"
                    }
                },
                Serilog = new
                {
                    Using = new[] { "Serilog.Sinks.Console", "Serilog.Sinks.RollingFile" },
                    MinimumLevel = new
                    {
                        Default = "Verbose",
                        Override = new
                        {
                            Microsoft = "Warning"
                        }
                    },
                    WriteTo = new object[]
                    {
                        new
                        {
                            Name = "Console",
                            Args = new
                            {
                                outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] <{SourceContext}> {Message}{NewLine}{Exception}"
                            }
                        },
                        new
                        {
                            Name = "RollingFile",
                            Args = new
                            {
                                pathFormat = path,
                                fileSizeLimitBytes = 20000000,
                                retainedFileCountLimit = 5,
                                outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] <{SourceContext}> {Message}{NewLine}{Exception}"
                            }
                        }
                    },
                    Properties = new
                    {
                        Application = service
                    }
                }
            };

            return o.ToConfigurationArgs();
        }

        public static ILogger BuildRootLogger(string service)
        {
            var args = GetSerilogConfigurationArgs(service, MakeDefaultLogPath(service));

            var appConfig = BuildConfiguration(args);

            var logConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(appConfig)
                .Enrich.FromLogContext();

            var logger = logConfig.CreateLogger();

            logger.Information("Root logger is ensured");

            return logger;
        }

        public static string MakeDefaultLogPath(string service)
        {
            return Path.Combine(AppContext.BaseDirectory, "logs", service, "{Date}.log");
        }

        public static IConfiguration BuildConfiguration(string[] args, string jsonFile = "appsettings")
        {
            var environmentName = Environment.GetEnvironmentVariable("Hosting:Environment");

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory);

            if (!string.IsNullOrWhiteSpace(jsonFile))
            {
                builder.AddJsonFile($"{jsonFile}.json", true);

                if (!string.IsNullOrWhiteSpace(environmentName))
                    builder.AddJsonFile($"{jsonFile}.{environmentName}.json", true);
            }

            builder.AddEnvironmentVariables();

            if (args != null && args.Any())
                builder.AddCommandLine(args);

            return builder.Build();
        }

    }
}
