using System;
using System.Threading.Tasks;
using DocaLabs.Qa;
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

        protected override async Task Given()
        {
            TestEnvironmentSetup.StartClientAgent();
            TestEnvironmentSetup.StartServiceAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        protected override async Task When()
        {
            await Helpers.ExecuteSuccessfulRequests(Service);

            TestEnvironmentSetup.StopClientAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));

            await Helpers.ExecuteFailingRequest(Service);

            TestEnvironmentSetup.StartClientAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        [Then]
        public async Task It_should_execute_requests_after_client_agent_restarted()
        {
            await Helpers.ExecuteSuccessfulRequests(Service);
        }
    }
}