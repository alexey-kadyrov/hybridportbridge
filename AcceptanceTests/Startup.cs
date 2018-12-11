using AcceptanceTests.Service;
using Microsoft.AspNetCore.Builder;

namespace AcceptanceTests
{
    internal class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (context, next) => await TestService.ProcessRequest(context, next));
        }
    }
}

