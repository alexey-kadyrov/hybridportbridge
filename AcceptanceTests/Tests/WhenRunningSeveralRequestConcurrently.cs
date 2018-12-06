using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTests.Client;
using DocaLabs.Qa;
using FluentAssertions;
using NUnit.Framework;
using Polly;
using Refit;

namespace AcceptanceTests.Tests
{
    [TestFixture]
    public class WhenRunningSeveralRequestConcurrently : BehaviorDrivenTest
    {
        private static IService _client1;
        private static IService _client2;

        private static Task<TestOutcome>[] _results;
        private static long _elapsedMilliseconds;

        protected override Task Given()
        {
            _client1 = ClientFactory.CreateRequest();
            _client2 = ClientFactory.CreateRequest();

            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            var timer = Stopwatch.StartNew();

            _results = new []
            {
                Do("Post-1", 50, i => Post(i, _client1)),
                Do("Get-1", 50, i => Get(_client1)),
                Do("Post-2", 50, i => Post(i, _client2)),
                Do("Get-2", 50, i => Get(_client2)),
                Do("Mix-1", 50, i => Mix(i, _client1)),
                Do("Mix-2", 50, i => Mix(i, _client2)),
            };

            await Task.WhenAll(_results);

            timer.Stop();

            _elapsedMilliseconds = timer.ElapsedMilliseconds;
        }

        [Then]
        public void Is_should_complete_all_requests_successfully()
        {
            var message = string.Join(Environment.NewLine, _results.Select(t => t.Result));

            var total = _results.Sum(t => t.Result.TotalIterations);
            var failed = _results.Sum(t => t.Result.FailedIterations);
            var retries = _results.Sum(t => t.Result.Retries);

            string preamble;
            // with retries there shouldn't be any failures, there is real networking goes on
            if (failed == 0)
            {
                preamble = $"Test completed for {total} iterations in {_elapsedMilliseconds} milliseconds. Retries={retries}.";
                TestContext.WriteLine($"{preamble}{Environment.NewLine}{message}");
                Assert.Pass(preamble);
            }
            else if(1.0 - (double)retries / total < 99.95 )
            {
                preamble = $"Too many retries. Retried {retries} times out of {total} iterations, which gives {(1.0 - (double)retries / total) * 100.0}% Success Rate, Run for {_elapsedMilliseconds} milliseconds.";
                TestContext.WriteLine($"{preamble}{Environment.NewLine}{message}");
                Assert.Fail(preamble);
            }
            else
            {
                preamble = $"Fail {failed} times out of {total} iterations, which gives {(1.0 - (double) failed / total) * 100.0}% Success Rate, Run for {_elapsedMilliseconds} milliseconds. Retries={retries}.";
                TestContext.WriteLine($"{preamble}{Environment.NewLine}{message}");
                Assert.Fail(preamble);
            }
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
                    var ii = i;
                    
                    await Policy
                        .Handle<ApiException>()
                        .RetryAsync(3, (exception, retry) => outcome.Retries ++)
                        .ExecuteAndCaptureAsync(() => action(ii));                    
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
            switch (i % 2)
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
            }
        }
        
        private struct TestOutcome
        {
            public string TestCase;
            public int TotalIterations;
            public int FailedIterations;
            public int Retries;
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
