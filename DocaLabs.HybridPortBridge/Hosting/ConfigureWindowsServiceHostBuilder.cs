using System;
using DocaLabs.HybridPortBridge.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DocaLabs.HybridPortBridge.Hosting
{
    public static class ConfigureWindowsServiceHostBuilder
    {
        public static IHostBuilder Configure(string[] args, Func<IConfiguration, IForwarder> forwarderFactory)
        {
            var builder = new HostBuilder()

                .ConfigureAppConfiguration((context, cfgBuilder) =>
                {
                    cfgBuilder.BuildConfiguration(args);
                })
                
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(forwarderFactory(context.Configuration));
                    
                    services.AddHostedService<WindowsServiceAgentHost>();
                });
            
            return builder;
        }
    }
}