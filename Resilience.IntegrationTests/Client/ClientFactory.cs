using System;
using System.Net.Http;
using Refit;

namespace Resilience.IntegrationTests.Client
{
    internal static class ClientFactory
    {
        public static IService CreateRequest()
        {
            var baseAddress = new Uri(TestEnvironmentSetup.ServiceBaseAddressForClientAgent);

            // Ensure that each test gets it's own http client as their setting vary between tests
            return RestService.For<IService>(new HttpClient
            {
                BaseAddress = baseAddress
            });
        }
    }
}
