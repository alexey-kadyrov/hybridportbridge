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
                Do("Post-1", 2000, i => Post(i, request1)),
                Do("Get-1", 2000, i => Get(request1)),
                Do("Post-2", 2000, i => Post(i, request2)),
                Do("Get-2", 2000, i => Get(request2)),
                Do("Post-with-client-cert", 2000, i => Post(i, requestWithClientCert)),
                Do("Get-with-client-cert", 2000, i => Get(requestWithClientCert)),
                Do("Mix-1", 1000, i => Mix(i, request1)),
                Do("Mix-2", 1000, i => Mix(i, request2)),
                Do("Mix-with-client-cert", 1000, i => Mix(i, requestWithClientCert)),

                // be conservative here in order not to exhaust the socket connections
                Do("IndividualMix-1", 50, IndividualMix),
                Do("IndividualMix-2", 50, IndividualMix)
            };

            await Task.WhenAll(tasks);

            overallTimer.Stop();

            Console.WriteLine(string.Join(Environment.NewLine, tasks.Select(t => t.Result)));

            var total = tasks.Sum(t => t.Result.TotalIterations);
            var failed = tasks.Sum(t => t.Result.FailedIterations);

            if (failed > 0)
                Assert.Fail($"Fail {failed} times out of {total} iterations, which gives {(1.0 - (double)failed/total) * 100.0}% Success Rate, Run for {overallTimer.ElapsedMilliseconds} milliseconds");
            else
                Assert.Pass($"Test completed for {total} iterations in {overallTimer.ElapsedMilliseconds} milliseconds");
        }

        private static async Task<TestOutcome> Do(string testCase, int iterations, Func<int, Task> action)
        {
            var outcome = new TestOutcome
            {
                TestCase = testCase,
                TotalIterations = iterations,
                FirstFailed = int.MaxValue,
                LastFailed = int.MinValue
            };

            var timer = Stopwatch.StartNew();

            for (var i = 0; i < iterations; i++)
            {
                try
                {
                    await action(i);
                }
                catch (Exception e)
                {
                    if (outcome.FirstFailed > i)
                        outcome.FirstFailed = i;

                    if (outcome.LastFailed < i)
                        outcome.LastFailed = i;

                    outcome.FailedIterations++;

                    if (string.IsNullOrWhiteSpace(outcome.FirstError))
                        outcome.FirstError = e.ToString();
                }
            }

            timer.Stop();

            outcome.ElapsedMilliseconds = timer.ElapsedMilliseconds;

            return outcome;
        }

        private static async Task Get(IService request)
        {
            var result = await request.GetProductAsync(42);

            result.Should().NotBeNull();
            result.Id.Should().Be(42);
            result.Category.Should().Be("Nothing");
            result.Name.Should().Be("Product");
            result.Price.Should().Be(1.99M);
        }

        private static async Task Post(int i, IService request)
        {
            var result = await request.PostProductAsync(new Product
            {
                Id = i,
                Category = "Hello",
                Name = "World",
                Price = 9.49M
            });

            result.Should().NotBeNull();
            result.Id.Should().Be(i);
            result.Category.Should().Be("Hello");
            result.Name.Should().Be("World");
            result.Price.Should().Be(9.49M);
        }

        private static async Task Mix(int i, IService request)
        {
            switch (i % 3)
            {
                case 0:
                {
                    var result = await request.GetProductAsync(42);

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

        private static async Task IndividualMix(int i)
        {
            switch (i % 8)
            {
                case 0:
                {
                    var request = ClientFactory.CreateRequest();

                    var result = await request.GetProductAsync(42);

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

        public struct TestOutcome
        {
            public string TestCase;
            public int TotalIterations;
            public int FailedIterations;
            public int FirstFailed;
            public int LastFailed;
            public string FirstError;
            public long ElapsedMilliseconds;

            public override string ToString()
            {
                var message = $"{TestCase} task completed in {ElapsedMilliseconds} milliseconds for {TotalIterations} iterations, {(double) ElapsedMilliseconds / TotalIterations} per iteration";

                if (FailedIterations <= 0)
                    return message;

                message += Environment.NewLine + $"Failed {FailedIterations} times with first failure in {FirstFailed} and last in {LastFailed} with first error:";
                message += Environment.NewLine + FirstError;

                return message;
            }
        }
    }
}