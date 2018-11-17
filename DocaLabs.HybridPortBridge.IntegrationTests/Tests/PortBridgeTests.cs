using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.IntegrationTests.Service;
using FluentAssertions;
using Microsoft.Extensions.Http;
using NUnit.Framework;
using Refit;

namespace DocaLabs.HybridPortBridge.IntegrationTests.Tests
{
    [TestFixture]
    public class PortBridgeTests
    {
        [Test]
        public async Task Should_post_and_get_successfull_response()
        {
            var request = CreateDefaultRestRequest();

            var result = await request.PostProductAsync(new Product
            {
                Id = 1,
                Category = "Hello",
                Name = "World",
                Price = 9.49M
            });

            result.Should().NotBeNull();
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Category.Should().Be("Hello");
            result.Name.Should().Be("World");
            result.Price.Should().Be(9.49M);
        }

        [Test]
        public async Task Should_get_successfull_response()
        {
            var request = CreateDefaultRestRequest();

            var result = await request.GetProductAsync(42);

            result.Should().NotBeNull();
            result.Should().NotBeNull();
            result.Id.Should().Be(42);
            result.Category.Should().Be("Nothing");
            result.Name.Should().Be("Product");
            result.Price.Should().Be(1.99M);
        }

        //[Test]
        //public async Task Should_process_successfully_large_requests()
        //{
        //    var request = CreateDefaultRestRequest();

        //    const int interations = 50;

        //    var timer = Stopwatch.StartNew();

        //    for (var i = 0; i < interations; i++)
        //    {
        //        var length = 500000 + i * 3;

        //        var data = Encoding.UTF8.GetString(Utils.GenerateRandomString(length));

        //        try
        //        {
        //            var result = await request.Post("large")
        //                .SuppressHttpException()
        //                .PlainTextContent(data)
        //                .Async()
        //                .ReadString();

        //            result.Should().NotBeNull();
        //            result.StatusCode.Should().Be(HttpStatusCode.OK);
        //            result.Value.Should().NotBeNull();
        //            result.Value.Should().NotBeNullOrWhiteSpace();
        //            result.Value.Should().Be(data);
        //        }
        //        catch
        //        {
        //            Console.WriteLine($"Failed on iteration {i}, (length={length})");
        //            throw;
        //        }
        //    }

        //    timer.Stop();

        //    Console.WriteLine($"Large response task completed in {timer.ElapsedMilliseconds} milliseconds, {(double)timer.ElapsedMilliseconds / interations} per iteration");
        //}

        //[Test]
        //public async Task Should_get_not_found_response()
        //{
        //    var request = CreateDefaultRestRequest();

        //    var result = await request.Get("products/100")
        //        .SuppressHttpException()
        //        .Async()
        //        .ReadJson<Product>();

        //    result.Should().NotBeNull();
        //    result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        //}

        private static IService CreateDefaultRestRequest()
        {
            var baseAddress = new Uri(TestSetup.EchoBaseAddress);

            ServicePointManager.FindServicePoint(baseAddress, null).ConnectionLeaseTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

            // Ensure that each test gets it's own http client as their setting vary between tests
            return RestService.For<IService>(new HttpClient
            {
                BaseAddress = baseAddress
            });
        }
    }
}