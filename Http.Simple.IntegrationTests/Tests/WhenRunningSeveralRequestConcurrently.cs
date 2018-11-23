using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DocaLabs.Qa;
using FluentAssertions;
using Http.Simple.IntegrationTests.Client;
using NUnit.Framework;

namespace Http.Simple.IntegrationTests.Tests
{
    [TestFixture]
    public class WhenRunningSeveralRequestConcurrently : BehaviorDrivenTest
    {
        private static IService _client1;
        private static IService _client2;
        private static IService _clientWithCert;

        private static Task<TestOutcome>[] _results;
        private static long _elapsedMilliseconds;

        protected override Task Given()
        {
            _client1 = ClientFactory.CreateRequest();
            _client2 = ClientFactory.CreateRequest();
            _clientWithCert = ClientFactory.CreateRequestWithClientCertificate();

            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            var timer = Stopwatch.StartNew();

            _results = new []
            {
                Do("Post-1", 2000, i => Post(i, _client1)),
                Do("Get-1", 2000, i => Get(_client1)),
                Do("Post-2", 2000, i => Post(i, _client2)),
                Do("Get-2", 2000, i => Get(_client2)),
                Do("Post-with-client-cert", 2000, i => Post(i, _clientWithCert)),
                Do("Get-with-client-cert", 2000, i => Get(_clientWithCert)),
                Do("Mix-1", 1000, i => Mix(i, _client1)),
                Do("Mix-2", 1000, i => Mix(i, _client2)),
                Do("Mix-with-client-cert", 1000, i => Mix(i, _clientWithCert)),

                // be conservative here in order not to exhaust the socket connections
                Do("IndividualMix-1", 50, IndividualMix),
                Do("IndividualMix-2", 50, IndividualMix)
            };

            await Task.WhenAll(_results);

            timer.Stop();

            _elapsedMilliseconds = timer.ElapsedMilliseconds;
        }

        [Then]
        public void Is_should_complete_all_requests_successfully()
        {
            Console.WriteLine(string.Join(Environment.NewLine, _results.Select(t => t.Result)));

            var total = _results.Sum(t => t.Result.TotalIterations);
            var failed = _results.Sum(t => t.Result.FailedIterations);

            if (failed > 0)
                Assert.Fail($"Fail {failed} times out of {total} iterations, which gives {(1.0 - (double)failed / total) * 100.0}% Success Rate, Run for {_elapsedMilliseconds} milliseconds");
            else
                Assert.Pass($"Test completed for {total} iterations in {_elapsedMilliseconds} milliseconds");
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
                        outcome.FirstError = e.Message;
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

        private struct TestOutcome
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
                var message = $"{TestCase} task completed in {ElapsedMilliseconds} milliseconds for {TotalIterations} iterations, {(double)ElapsedMilliseconds / TotalIterations} per iteration";

                if (FailedIterations <= 0)
                    return message;

                message += Environment.NewLine + $"Failed {FailedIterations} times with first failure in {FirstFailed} and last in {LastFailed} with first error:";
                message += Environment.NewLine + FirstError;

                return message;
            }
        }
    }
}
