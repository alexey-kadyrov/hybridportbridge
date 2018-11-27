using System;
using System.Threading.Tasks;
using DocaLabs.Qa;
using FluentAssertions;
using NUnit.Framework;
using Resilience.IntegrationTests.Client;

namespace Resilience.IntegrationTests.Tests
{
    [TestFixture]
    public class WhenClientAgentStopsAndThenStartsAgain : ServiceBehaviorDrivenTest<IService>
    {
        protected override async Task Cleanup()
        {
            TestEnvironmentSetup.StopClientAgent();
            TestEnvironmentSetup.StopServiceAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));

            await base.Cleanup();
        }

        protected override Task Given()
        {
            TestEnvironmentSetup.StartClientAgent();
            TestEnvironmentSetup.StartServiceAgent();

            return Task.CompletedTask;
        }

        protected override async Task When()
        {
            await ExecuteSuccessfulRequest();

            TestEnvironmentSetup.StopClientAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));

            await ExecuteFailingRequest();

            TestEnvironmentSetup.StartClientAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        [Then]
        public async Task It_should_execute_request_after_client_agent_restarted()
        {
            await ExecuteSuccessfulRequest();
        }

        private async Task ExecuteSuccessfulRequest()
        {
            var result = await Service.PostProductAsync(new Product
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

        private Task ExecuteFailingRequest()
        {
            var exception = Assert.CatchAsync(() => Service.PostProductAsync(new Product
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