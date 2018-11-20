using System;
using System.Threading.Tasks;
using DocaLabs.Qa;
using Microsoft.AspNetCore.Http;

namespace Resilience.IntegrationTests.Service
{
    internal class TestService
    {
        public static async Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            // emulate some work
            await Task.Delay(10);

            if (context.Request.IsPostPath("/api/echo/products"))
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = context.Request.ContentType;
                await context.Request.Body.CopyToAsync(context.Response.Body);
                await context.Response.Body.FlushAsync();
            }
            else
            {
                await next.Invoke();
            }
        }
    }
}