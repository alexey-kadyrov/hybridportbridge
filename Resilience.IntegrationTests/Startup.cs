using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Resilience.IntegrationTests.Service;

namespace Resilience.IntegrationTests
{
    internal class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory logger)
        {
            app.Use(async (context, next) => await TestService.ProcessRequest(context, next));
        }
    }
}

