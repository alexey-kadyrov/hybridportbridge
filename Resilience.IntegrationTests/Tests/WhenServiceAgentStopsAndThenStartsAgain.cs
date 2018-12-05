using System;
using System.Threading.Tasks;
using DocaLabs.Qa;
using NUnit.Framework;
using Resilience.IntegrationTests.Client;

namespace Resilience.IntegrationTests.Tests
{
    [TestFixture]
    public class WhenServiceAgentStopsAndThenStartsAgain : ServiceBehaviorDrivenTest<IService>
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
            await Helpers.ExecuteSuccessfulRequests(Service);

            TestEnvironmentSetup.StopServiceAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));

            await Helpers.ExecuteFailingRequest(Service);

            TestEnvironmentSetup.StartServiceAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        [Then]
        public async Task It_should_execute_requests_after_service_agent_restarted()
        {
            await Helpers.ExecuteSuccessfulRequests(Service);
        }
    }
}
