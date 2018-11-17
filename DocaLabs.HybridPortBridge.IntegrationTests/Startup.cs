using DocaLabs.HybridPortBridge.IntegrationTests.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace DocaLabs.HybridPortBridge.IntegrationTests
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory logger)
        {
            app.Use(async (context, next) => await TestService.ProcessRequest(context, next));
        }
    }
}

