﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Polly;
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
            
                var errorMessage = $"Error{Environment.NewLine}";

                var ii = i;
                
                await Policy
                    .Handle<Exception>()
                    .RetryAsync(3, (exception, r) => errorMessage += $"{Environment.NewLine}Failed in {ii}:{r} with: {exception}")
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

                Assert.IsNotNull(result, errorMessage);
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
