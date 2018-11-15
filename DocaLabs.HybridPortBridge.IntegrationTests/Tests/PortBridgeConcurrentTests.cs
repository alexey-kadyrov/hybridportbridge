using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.IntegrationTests.Service;
using NUnit.Framework;

namespace DocaLabs.HybridPortBridge.IntegrationTests.Tests
{
    [TestFixture]
    public class PortBridgeConcurrentTests
    {
        [Test]
        public async Task Should_successfully_execute_mix_of_concurrent_requests()
        {
            var request = CreateDefaultRestRequest();
            var request33 = CreateDefaultRestRequest();
            var requestWithClientCert = CreateDefaultRestRequestWithClientCert();

            var overallTimer = Stopwatch.StartNew();

            var tasks = new[]
            {
                Post(2000, request),
                Get(2000, request),
                Post(2000, request33),
                Get(2000, request33),
                Post(2000, requestWithClientCert),
                Get(2000, requestWithClientCert),
                Mix(1000, request),
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

        private static async Task<string> Get(int iterations, IRestRequest request)
        {
            var timer = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                var result = await request.Get("products/42")
                    .SuppressHttpException()
                    .Async()
                    .ReadJson<Product>();

                result.Should().NotBeNull();
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.Value.Should().NotBeNull();
                result.Value.Id.Should().Be(42);
                result.Value.Category.Should().Be("Nothing");
                result.Value.Name.Should().Be("Product");
                result.Value.Price.Should().Be(1.99M);
            }

            timer.Stop();

            return $"Get task completed in {timer.ElapsedMilliseconds} milliseconds, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
        }

        private static async Task<string> Post(int iterations, IRestRequest request)
        {
            var timer = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                var result = await request.Post("products")
                    .SuppressHttpException()
                    .JsonContent(new Product
                    {
                        Id = i,
                        Category = "Hello",
                        Name = "World",
                        Price = 9.49M
                    })
                    .Async()
                    .ReadJson<Product>();

                result.Should().NotBeNull();
                result.StatusCode.Should().Be(HttpStatusCode.OK);
                result.Value.Should().NotBeNull();
                result.Value.Id.Should().Be(i);
                result.Value.Category.Should().Be("Hello");
                result.Value.Name.Should().Be("World");
                result.Value.Price.Should().Be(9.49M);
            }

            timer.Stop();

            return $"Post task completed in {timer.ElapsedMilliseconds} milliseconds, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
        }

        private static async Task<string> Mix(int iterations, IRestRequest request)
        {
            var timer = Stopwatch.StartNew();

            var random = new Random();

            for (var i = 0; i < iterations; i++)
            {
                switch (random.Next(3))
                {
                    case 0:
                    {
                        var result = await request.Get("products/42")
                            .SuppressHttpException()
                            .Async()
                            .ReadJson<Product>();

                        result.Should().NotBeNull();
                        result.StatusCode.Should().Be(HttpStatusCode.OK);
                        result.Value.Should().NotBeNull();
                        result.Value.Id.Should().Be(42);
                        result.Value.Category.Should().Be("Nothing");
                        result.Value.Name.Should().Be("Product");
                        result.Value.Price.Should().Be(1.99M);
                        break;
                    }
                    case 1:
                    {
                        var result = await request.Post("products")
                            .SuppressHttpException()
                            .JsonContent(new Product
                            {
                                Id = i,
                                Category = "Hello",
                                Name = "World",
                                Price = 9.49M
                            })
                            .Async()
                            .ReadJson<Product>();

                        result.Should().NotBeNull();
                        result.StatusCode.Should().Be(HttpStatusCode.OK);
                        result.Value.Should().NotBeNull();
                        result.Value.Id.Should().Be(i);
                        result.Value.Category.Should().Be("Hello");
                        result.Value.Name.Should().Be("World");
                        result.Value.Price.Should().Be(9.49M);
                        break;
                    }
                    case 2:
                    {
                        var length = 500000 + i;

                        var result = await request.Get($"large?ll={length}")
                            .SuppressHttpException()
                            .Async()
                            .ReadString();

                        result.Should().NotBeNull();
                        result.StatusCode.Should().Be(HttpStatusCode.OK);
                        result.Value.Should().NotBeNull();
                        result.Value.Should().NotBeNullOrWhiteSpace();
                        result.Value.Should().HaveLength(length);
                        break;
                    }
                }
            }

            timer.Stop();

            return $"Mix task completed in {timer.ElapsedMilliseconds} milliseconds, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
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
                    case 3:
                    {
                            var request = value == 0
                                ? CreateDefaultRestRequest()
                                : CreateDefaultRestRequestWithClientCert();

                            var result = await request.Get("products/42")
                                .SuppressHttpException()
                                .Async()
                                .ReadJson<Product>();

                            result.Should().NotBeNull();
                            result.StatusCode.Should().Be(HttpStatusCode.OK);
                            result.Value.Should().NotBeNull();
                            result.Value.Id.Should().Be(42);
                            result.Value.Category.Should().Be("Nothing");
                            result.Value.Name.Should().Be("Product");
                            result.Value.Price.Should().Be(1.99M);
                            break;
                        }
                    case 1:
                    case 4:
                        {
                            var request = value == 1
                                ? CreateDefaultRestRequest()
                                : CreateDefaultRestRequestWithClientCert();

                            var result = await request.Post("products")
                                .SuppressHttpException()
                                .JsonContent(new Product
                                {
                                    Id = i,
                                    Category = "Hello",
                                    Name = "World",
                                    Price = 9.49M
                                })
                                .Async()
                                .ReadJson<Product>();

                            result.Should().NotBeNull();
                            result.StatusCode.Should().Be(HttpStatusCode.OK);
                            result.Value.Should().NotBeNull();
                            result.Value.Id.Should().Be(i);
                            result.Value.Category.Should().Be("Hello");
                            result.Value.Name.Should().Be("World");
                            result.Value.Price.Should().Be(9.49M);
                            break;
                        }
                    case 2:
                    case 5:
                        {
                            var request = value == 2
                                ? CreateDefaultRestRequest()
                                : CreateDefaultRestRequestWithClientCert();

                            var length = 500000 + i;

                            var result = await request.Get($"large?ll={length}")
                                .SuppressHttpException()
                                .Async()
                                .ReadString();

                            result.Should().NotBeNull();
                            result.StatusCode.Should().Be(HttpStatusCode.OK);
                            result.Value.Should().NotBeNull();
                            result.Value.Should().NotBeNullOrWhiteSpace();
                            result.Value.Should().HaveLength(length);
                            break;
                        }
                    case 6:
                        {
                            var request = value == 2
                                ? CreateDefaultRestRequest()
                                : CreateDefaultRestRequestWithClientCert();

                            var length = 500000 + i;

                            var data = Encoding.UTF8.GetString(Utils.GenerateRandomString(length));

                            var result = await request.Post("large")
                                .SuppressHttpException()
                                .PlainTextContent(data)
                                .Async()
                                .ReadString();

                            result.Should().NotBeNull();
                            result.StatusCode.Should().Be(HttpStatusCode.OK);
                            result.Value.Should().NotBeNull();
                            result.Value.Should().NotBeNullOrWhiteSpace();
                            result.Value.Should().Be(data);

                            break;
                        }
                    case 7:
                    {
                        var request = CreateDefaultRestRequestWithClientCert(TestSetup.ServerCertificate, TestSetup.ServerCertificatePassword);

                        Assert.ThrowsAsync<HttpRequestException>(async () => await request.Get("products/42")
                            .Async()
                            .ReadJson<Product>());
                        break;
                    }
                }
            }

            timer.Stop();

            return $"Individual Mix task completed in {timer.ElapsedMilliseconds} milliseconds, {(double)timer.ElapsedMilliseconds / iterations} per iteration";
        }

        private static IRestRequest CreateDefaultRestRequest()
        {
            // discovery is used here in order to ensure that each test gets it's own http client as their setting vary between tests
            return RestRequestFactory.Create(
                Guid.NewGuid().ToString(),
                new RestRequestOptions(),
                () => new HttpClientOptions
                {
                    EndpointDiscovery = new ConfigurableEndpointDiscovery(TestSetup.EchoBaseAddress),
                    ConnectionLeaseTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds
                });
        }

        private static IRestRequest CreateDefaultRestRequest33()
        {
            // discovery is used here in order to ensure that each test gets it's own http client as their setting vary between tests
            return RestRequestFactory.Create(
                Guid.NewGuid().ToString(),
                new RestRequestOptions(),
                () => new HttpClientOptions
                {
                    EndpointDiscovery = new ConfigurableEndpointDiscovery(TestSetup.EchoBaseAddress33),
                    ConnectionLeaseTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds
                });
        }

        private static IRestRequest CreateDefaultRestRequestWithClientCert(string certificate = TestSetup.ClientCertificate, string password = TestSetup.ClientCertificatePassword)
        {
            // discovery is used here in order to ensure that each test gets it's own http client as their setting vary between tests
            return RestRequestFactory.Create(
                Guid.NewGuid().ToString(),
                new RestRequestOptions(),
                () => new HttpClientOptions
                {
                    EndpointDiscovery = new ConfigurableEndpointDiscovery(TestSetup.EchoBaseAddressWithClientCertificateRequirements),
                    ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) =>
                        string.Equals(certificate2.Thumbprint, TestSetup.ServerCertificateThumbprint, StringComparison.OrdinalIgnoreCase),
                    ClientCertificate = new X509Certificate2(Convert.FromBase64String(certificate), password,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet),
                    ConnectionLeaseTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds
                });
        }
    }
}