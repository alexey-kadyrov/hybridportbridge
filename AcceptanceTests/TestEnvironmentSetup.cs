using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using NUnit.Framework;

namespace AcceptanceTests
{
    [SetUpFixture]
    public class TestEnvironmentSetup
    {
        public const string ServiceBaseAddressForClientAgent = "http://localhost:5021";
        private const string ServiceBaseAddress = "http://localhost:5011/";

        private IWebHost _serviceHost;

        [OneTimeSetUp]
        public async Task Setup()
        {
            try
            {
                TestContext.WriteLine("Setting up test environment...");

                ServicePointManager.FindServicePoint(ServiceBaseAddressForClientAgent, null).ConnectionLeaseTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

                await StartService();
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
                throw;
            }
        }

        [OneTimeTearDown]
        public async Task Cleanup()
        {
            TestContext.WriteLine("Cleaning up test environment...");

            await StopService();
        }

        private async Task StartService()
        {
            try
            {
                _serviceHost = WebHost.CreateDefaultBuilder()
                    .UseUrls(ServiceBaseAddress)
                    .UseStartup<Startup>()
                    .Build();

                await _serviceHost.StartAsync();
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
            }
        }

        private async Task StopService()
        {
            try
            {
                await _serviceHost.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
            }
        }
    }
}
