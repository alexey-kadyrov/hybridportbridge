using System;
using System.Net.Http;
using System.Threading.Tasks;
using DocaLabs.Qa;
using FluentAssertions;
using NUnit.Framework;
using Resilience.IntegrationTests.Client;

namespace Resilience.IntegrationTests.Tests
{
    public class WhenClientAgentStopsAndThenStartsAgain : BehaviorDrivenTest
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

            TestEnvironmentSetup.StopClientAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));

            await ExecuteFailingRequest();

            TestEnvironmentSetup.StartClientAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        [Then]
        public async Task It_should_execute_request_after_client_agent_restarted()
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
            var exception = Assert.CatchAsync(() => _request.PostProductAsync(new Product
            {
                Id = 1,
                Category = "Hello",
                Name = "World",
                Price = 9.49M
            }));

            exception.Should().NotBeNull();

            return Task.CompletedTask;
        }
    }
}