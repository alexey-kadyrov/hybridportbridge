using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Http.Simple.IntegrationTests.Client;
using NUnit.Framework;

namespace Http.Simple.IntegrationTests.Tests
{
    [TestFixture]
    public class PortBridgeConcurrentTests
    {
        [Test]
        public async Task Should_successfully_execute_mix_of_concurrent_requests()
        {
            var request1 = ClientFactory.CreateRequest();
            var request2 = ClientFactory.CreateRequest();
            var requestWithClientCert = ClientFactory.CreateRequestWithClientCertificate();

            var overallTimer = Stopwatch.StartNew();

            var tasks = new[]
            {
                Post(2000, request1),
                Get(2000, request1),
                Post(2000, request2),
                Get(2000, request2),
                Post(2000, requestWithClientCert),
                Get(2000, requestWithClientCert),
                Mix(1000, request1),
                Mix(1000, request2),
                Mix(1000, requestWithClientCert),

                // be conservative here in order not to exhaust the socket connections
                IndividualMix(50), 
                IndividualMix(50)
            };

            await Task.WhenAll(tasks);

            overallTimer.Stop();

            var message = $"Test completed in {overallTimer.ElapsedMilliseconds} milliseconds{Environment.NewLine}";

            message += string.Join(Environment.NewLine, tasks.Select(t => t.Result));

            Console.WriteLine(message);

            Assert.Pass();
        }

        private static async Task<string> Get(int iterations, IService request)
        {
            var timer = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                var result = await request.GetProductAsync(42);

                result.Should().NotBeNull();
                result.Should().NotBeNull();
                result.Id.Should().Be(42);
                result.Category.Should().Be("Nothing");
                result.Name.Should().Be("Product");
                result.Price.Should().Be(1.99M);
            }

            timer.Stop();

            return $"Get task completed in {timer.ElapsedMilliseconds} milliseconds for {iterations} iterations, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
        }

        private static async Task<string> Post(int iterations, IService request)
        {
            var timer = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                var result = await request.PostProductAsync(new Product
                {
                    Id = i,
                    Category = "Hello",
                    Name = "World",
                    Price = 9.49M
                });

                result.Should().NotBeNull();
                result.Should().NotBeNull();
                result.Id.Should().Be(i);
                result.Category.Should().Be("Hello");
                result.Name.Should().Be("World");
                result.Price.Should().Be(9.49M);
            }

            timer.Stop();

            return $"Post task completed in {timer.ElapsedMilliseconds} milliseconds for {iterations} iterations, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
        }

        private static async Task<string> Mix(int iterations, IService request)
        {
            var timer = Stopwatch.StartNew();

            var random = new Random();

            for (var i = 0; i < iterations; i++)
            {
                switch (random.Next(3))
                {
                    case 0:
                    {
                        var result = await request.GetProductAsync(42);

                        result.Should().NotBeNull();
                        result.Should().NotBeNull();
                        result.Id.Should().Be(42);
                        result.Category.Should().Be("Nothing");
                        result.Name.Should().Be("Product");
                        result.Price.Should().Be(1.99M);
                        break;
                    }
                    case 1:
                    {
                        var result = await request.PostProductAsync(new Product
                        {
                            Id = i,
                            Category = "Hello",
                            Name = "World",
                            Price = 9.49M
                        });

                        result.Should().NotBeNull();
                        result.Should().NotBeNull();
                        result.Id.Should().Be(i);
                        result.Category.Should().Be("Hello");
                        result.Name.Should().Be("World");
                        result.Price.Should().Be(9.49M);
                        break;
                    }
                    case 2:
                    {
                        var data = LargeDataProvider.Next();

                        var result = await request.PostLargeDataAsync(new MemoryStream(data));

                        result.Should().NotBeNull();
                        (await LargeDataProvider.Compare(data, result)).Should().BeTrue();
                        break;
                    }
                }
            }

            timer.Stop();

            return $"Mix task completed in {timer.ElapsedMilliseconds} milliseconds for {iterations} iterations, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
        }

        private static async Task<string> IndividualMix(int iterations)
        {
            var timer = Stopwatch.StartNew();

            var random = new Random();

            for (var i = 0; i < iterations; i++)
            {
                var value = random.Next(8);

                switch (value)
                {
                    case 0:
                    {
                        var request = ClientFactory.CreateRequest();

                        var result = await request.GetProductAsync(42);

                        result.Should().NotBeNull();
                        result.Should().NotBeNull();
                        result.Id.Should().Be(42);
                        result.Category.Should().Be("Nothing");
                        result.Name.Should().Be("Product");
                        result.Price.Should().Be(1.99M);
                        break;
                    }
                    case 3:
                    {
                        var request = ClientFactory.CreateRequestWithClientCertificate();

                        var result = await request.GetProductAsync(42);

                        result.Should().NotBeNull();
                        result.Should().NotBeNull();
                        result.Id.Should().Be(42);
                        result.Category.Should().Be("Nothing");
                        result.Name.Should().Be("Product");
                        result.Price.Should().Be(1.99M);
                        break;
                    }
                    case 1:
                    {
                        var request = ClientFactory.CreateRequest();

                        var result = await request.PostProductAsync(new Product
                        {
                            Id = i,
                            Category = "Hello",
                            Name = "World",
                            Price = 9.49M
                        });

                        result.Should().NotBeNull();
                        result.Should().NotBeNull();
                        result.Id.Should().Be(i);
                        result.Category.Should().Be("Hello");
                        result.Name.Should().Be("World");
                        result.Price.Should().Be(9.49M);
                        break;
                    }
                    case 4:
                    {
                        var request = ClientFactory.CreateRequestWithClientCertificate();

                        var result = await request.PostProductAsync(new Product
                        {
                            Id = i,
                            Category = "Hello",
                            Name = "World",
                            Price = 9.49M
                        });

                        result.Should().NotBeNull();
                        result.Should().NotBeNull();
                        result.Id.Should().Be(i);
                        result.Category.Should().Be("Hello");
                        result.Name.Should().Be("World");
                        result.Price.Should().Be(9.49M);
                        break;
                    }
                    case 2:
                    {
                        var request = ClientFactory.CreateRequest();

                        var data = LargeDataProvider.Next();

                        var result = await request.PostLargeDataAsync(new MemoryStream(data));

                        result.Should().NotBeNull();
                        (await LargeDataProvider.Compare(data, result)).Should().BeTrue();
                        break;
                    }
                    case 5:
                    {
                        var request = ClientFactory.CreateRequestWithClientCertificate();

                        var data = LargeDataProvider.Next();

                        var result = await request.PostLargeDataAsync(new MemoryStream(data));

                        result.Should().NotBeNull();
                        (await LargeDataProvider.Compare(data, result)).Should().BeTrue();
                        break;
                    }
                    case 6:
                    {
                        var request = ClientFactory.CreateRequestWithClientCertificate(TestEnvironmentSetup.ServerCertificate, TestEnvironmentSetup.ServerCertificatePassword);
                        Assert.ThrowsAsync<HttpRequestException>(async () => await request.GetProductAsync(42));
                        break;
                    }
                }
            }

            timer.Stop();

            return $"Individual Mix task completed in {timer.ElapsedMilliseconds} milliseconds for {iterations} iterations, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
        }
    }
}