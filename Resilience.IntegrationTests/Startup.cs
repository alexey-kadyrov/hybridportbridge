using Microsoft.AspNetCore.Builder;
using Resilience.IntegrationTests.Service;

namespace Resilience.IntegrationTests
{
    internal class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) => await TestService.ProcessRequest(context, next));
        }
    }
}

