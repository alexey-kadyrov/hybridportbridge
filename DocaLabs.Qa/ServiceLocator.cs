using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocaLabs.Qa
{
    public static class ServiceLocator
    {
        public static IServiceProvider Services { get; private set; }

        public static IConfiguration Configuration { get; private set; }

        public static void Initialize(Action<IServiceCollection, IConfiguration> builder, string[] args = null, string jsonFile = null)
        {
            Configuration = QaDefaults.BuildConfiguration(args, jsonFile);

            Services = BuildServiceProvider(builder);
        }

        private static IServiceProvider BuildServiceProvider(Action<IServiceCollection, IConfiguration> builder)
        {
            var services = new ServiceCollection();

            builder?.Invoke(services, Configuration);

            return services.BuildServiceProvider();
        }

        public static IServiceScope GetScope()
        {
            return Services.CreateScope();
        }
    }
}
