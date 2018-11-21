using Http.Simple.IntegrationTests.Service;
using Microsoft.AspNetCore.Builder;

namespace Http.Simple.IntegrationTests
{
    internal class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) => await TestService.ProcessRequest(context, next));
        }
    }
}

