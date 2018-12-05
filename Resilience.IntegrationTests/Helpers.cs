using System;
using System.Threading.Tasks;
using FluentAssertions;
using Polly;
using Refit;
using Resilience.IntegrationTests.Client;

namespace Resilience.IntegrationTests
{
    public static class Helpers
    {
        public static async Task ExecuteSuccessfulRequests(IService service)
        {
            for (var i = 0; i < 10; i++)
            {
                Product result = null;

                await Policy
                    .Handle<ApiException>()
                    .RetryAsync(3)
                    .ExecuteAndCaptureAsync(async () =>
                    {
                        result = await service.PostProductAsync(new Product
                        {
                            Id = 1,
                            Category = "Hello",
                            Name = "World",
                            Price = 9.49M
                        });
                    });

                result.Should().NotBeNull();
                result.Id.Should().Be(1);
                result.Category.Should().Be("Hello");
                result.Name.Should().Be("World");
                result.Price.Should().Be(9.49M);
            }
        }

        public static async Task ExecuteFailingRequest(IService service)
        {
            var failureCount = 0;

            await Policy
                .Handle<Exception>()
                .RetryAsync(3, (exception1, retry) => failureCount++)
                .ExecuteAndCaptureAsync(async () =>
                {
                    await service.PostProductAsync(new Product
                    {
                        Id = 1,
                        Category = "Hello",
                        Name = "World",
                        Price = 9.49M
                    });
                });

            failureCount.Should().Be(3);
        }
    }
}
