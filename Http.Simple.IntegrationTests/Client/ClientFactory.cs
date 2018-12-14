using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Refit;

namespace Http.Simple.IntegrationTests.Client
{
    internal static class ClientFactory
    {
        public static IService CreateFailingRequest()
        {
            var baseAddress = new Uri(TestEnvironmentSetup.FailingServiceBaseAddress);

            // Ensure that each test gets it's own http client as their setting vary between tests
            return RestService.For<IService>(new HttpClient
            {
                BaseAddress = baseAddress
            });
        }

        public static IService CreateRequest()
        {
            var baseAddress = new Uri(TestEnvironmentSetup.ServiceBaseAddressForClientAgent);

            // Ensure that each test gets it's own http client as their setting vary between tests
            return RestService.For<IService>(new HttpClient
            {
                BaseAddress = baseAddress
            });
        }

        public static IService CreateRequestForLocalhostEntity()
        {
            var baseAddress = new Uri(TestEnvironmentSetup.ServiceBaseAddressForLocalHostEntityPathClientAgent);

            // Ensure that each test gets it's own http client as their setting vary between tests
            return RestService.For<IService>(new HttpClient
            {
                BaseAddress = baseAddress
            });
        }

        public static IService CreateRequestWithClientCertificate(string certificate = TestEnvironmentSetup.ClientCertificate, string password = TestEnvironmentSetup.ClientCertificatePassword)
        {
            var baseAddress = new Uri(TestEnvironmentSetup.ServiceRequiringClientCertificateBaseAddressForClientAgent);

            // Ensure that each test gets it's own http client as their setting vary between tests

            if (string.IsNullOrWhiteSpace(certificate))
                return RestService.For<IService>(new HttpClient
                {
                    BaseAddress = baseAddress
                });

            return RestService.For<IService>(new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) =>
                    string.Equals(certificate2.Thumbprint, TestEnvironmentSetup.ServerCertificateThumbprint, StringComparison.OrdinalIgnoreCase),
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ClientCertificates = { new X509Certificate2(Convert.FromBase64String(certificate), password,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet) }
            })
            {
                BaseAddress = baseAddress
            });
        }
    }
}
