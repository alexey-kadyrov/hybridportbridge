using System;
using System.IO;
using System.Threading.Tasks;
using DocaLabs.Qa;
using Http.Simple.IntegrationTests.Client;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Http.Simple.IntegrationTests.Service
{
    internal static class TestService
    {
        public static async Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            try
            {
                // emulate some work
                await Task.Delay(10);

                if (context.Request.IsGetPath("/api/echo/products/42"))
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new Product
                    {
                        Id = 42,
                        Category = "Nothing",
                        Name = "Product",
                        Price = 1.99M
                    }));
                }
                else if (context.Request.IsPostPath("/api/echo/products"))
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = context.Request.ContentType;
                    await context.Request.Body.CopyToAsync(context.Response.Body);
                    await context.Response.Body.FlushAsync();
                }
                else if (context.Request.IsPostPath("/api/echo/large"))
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
            catch (Exception e)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "service.log"), e.ToString());
                throw;
            }
        }
    }
}