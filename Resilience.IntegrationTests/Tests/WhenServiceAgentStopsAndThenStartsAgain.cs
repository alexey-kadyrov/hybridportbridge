using System;
using System.Net.Http;
using System.Threading.Tasks;
using DocaLabs.Qa;
using FluentAssertions;
using NUnit.Framework;
using Refit;
using Resilience.IntegrationTests.Client;

namespace Resilience.IntegrationTests.Tests
{
    public class WhenServiceAgentStopsAndThenStartsAgain : BehaviorDrivenTest
    {
        private static IService _request;

        protected override Task Given()
        {
            TestEnvironmentSetup.StartClientAgent();
            TestEnvironmentSetup.StartServiceAgent();

            _request = ClientFactory.CreateRequest();

            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            await ExecuteSuccessfullRequest();

            TestEnvironmentSetup.StopServiceAgent();

            await Task.Delay(TimeSpan.FromSeconds(2));

            await ExecuteFailingRequest();

            TestEnvironmentSetup.StartServiceAgent();

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        [Then]
        public async Task It_should_execute_request_after_service_agent_restarted()
        {
            await ExecuteSuccessfullRequest();
        }

        private static async Task ExecuteSuccessfullRequest()
        {
            var result = await _request.PostProductAsync(new Product
            {
                Id = 1,
                Category = "Hello",
                Name = "World",
                Price = 9.49M
            });

            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Category.Should().Be("Hello");
            result.Name.Should().Be("World");
            result.Price.Should().Be(9.49M);
        }

        private static Task ExecuteFailingRequest()
        {
            Assert.ThrowsAsync<HttpRequestException>(() => _request.PostProductAsync(new Product
            {
                Id = 1,
                Category = "Hello",
                Name = "World",
                Price = 9.49M
            }));

            return Task.CompletedTask;
        }
    }
}
