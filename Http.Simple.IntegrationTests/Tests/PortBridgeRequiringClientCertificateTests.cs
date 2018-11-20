using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Http.Simple.IntegrationTests.Client;
using NUnit.Framework;
using Refit;

namespace Http.Simple.IntegrationTests.Tests
{
    [TestFixture]
    public class PortBridgeWithClientCertificateTests
    {
        [Test]
        public async Task Should_post_and_get_successful_response()
        {
            var request = ClientFactory.CreateRequestWithClientCertificate();

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
        public async Task Should_get_successful_response()
        {
            var request = ClientFactory.CreateRequestWithClientCertificate();

            var result = await request.GetProductAsync(42);

            result.Should().NotBeNull();
            result.Should().NotBeNull();
            result.Id.Should().Be(42);
            result.Category.Should().Be("Nothing");
            result.Name.Should().Be("Product");
            result.Price.Should().Be(1.99M);
        }

        [Test]
        public async Task Should_process_successfully_large_requests()
        {
            var request = ClientFactory.CreateRequestWithClientCertificate();

            const int iterations = 10;

            var timer = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                var data = LargeDataProvider.Next();

                try
                {
                    var result = await request.PostLargeDataAsync(new MemoryStream(data));

                    result.Should().NotBeNull();

                    (await LargeDataProvider.Compare(data, result)).Should().BeTrue();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed on iteration {i}, (length={data.Length}): {e}");
                    throw;
                }
            }

            timer.Stop();

            Console.WriteLine($"Large response task completed in {timer.ElapsedMilliseconds} milliseconds for {iterations} iterations, {(double)timer.ElapsedMilliseconds / iterations} per iteration");
        }

        [Test]
        public void Should_get_not_found_response()
        {
            var request = ClientFactory.CreateRequestWithClientCertificate();

            var result = Assert.ThrowsAsync<ApiException>(() => request.GetProductAsync(100));

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public void Should_fail_for_wrong_client_certificate()
        {
            var request = ClientFactory.CreateRequestWithClientCertificate(TestEnvironmentSetup.ServerCertificate, TestEnvironmentSetup.ServerCertificatePassword);

            Assert.ThrowsAsync<HttpRequestException>(async () => await request.GetProductAsync(42));
        }

        [Test]
        public void Should_fail_when_there_is_no_client_certificate()
        {
            var request = ClientFactory.CreateRequestWithClientCertificate(null, null);

            Assert.ThrowsAsync<HttpRequestException>(async () => await request.GetProductAsync(42));
        }
    }
}
