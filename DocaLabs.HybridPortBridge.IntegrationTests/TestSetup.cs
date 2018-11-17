using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using DocaLabs.Qa;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using NUnit.Framework;

namespace DocaLabs.HybridPortBridge.IntegrationTests
{
    [SetUpFixture]
    public class TestSetup
    {
        public const string EchoBaseAddressWithClientCertificateRequirements = "https://localhost:5020";
        public const string EchoBaseAddress = "http://localhost:5021";
        public const string EchoBaseAddress33 = "http://localhost:5031";
        public const string EchoServiceBaseAddress = "http://localhost:5011/";

        public const string ServiceNamespace = "";
        public const string AccessRuleName = "";
        public const string AccessRuleKey = "";

        public const string TargetHostEcho = "";
        public const string TargetHostEcho33 = "";

        public const string ClientCertificate =
            "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";
        public const string ClientCertificatePassword = "cOnIvhjFVs2g9g1bMiDhb2Nsh1NNYjKN";
        public const string ClientCertificateThumbprint = "88621C7F1DDF3E897BFCB7FF7F5EA888AF6F1A73";

        public const string ServerCertificate =
            "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";
        public const string ServerCertificatePassword = "3GdOT6mw9wE7dYk4JAPetTWTjHZ2aPgm";
        public const string ServerCertificateThumbprint = "F15F77C47E9865D3744A6BE68DDA75135572AEED";

        private const string ClientAgentName = "Azure.Relay.PortBridge.ClientAgent";
        private const string ServiceAgentName = "Azure.Relay.PortBridge.ServiceAgent";

        private static readonly string[] ServiceAgentArgs = QaDefaults
            .GetSerilogConfigurationArgs(ServiceAgentName, QaDefaults.MakeDefaultLogPath(ServiceAgentName))
            .MergeRange(new
            {
                AgentMetrics = new AgentMetricsOptions
                {
                    MetricsOptions =
                    {
                        DefaultContextLabel = "Azure Relay Port Bridge",
                        GlobalTags =
                        {
                            { "agent", "Service" }
                        },
                        Enabled = true,
                        ReportingEnabled = true
                    },
                    ReportingOptions =
                    {
                        ReportingFlushIntervalSeconds = 30,
                        ReportFile = Path.Combine(AppContext.BaseDirectory, "metrics-service.txt")
                    }
                }
            }.ToConfigurationArgs())
            .MergeRange(new
            {
                PortBridge = new ServiceAgentOptions
                {
                    ServiceNamespace = new ServiceNamespaceOptions
                    {
                        ServiceNamespace = ServiceNamespace,
                        AccessRuleName = AccessRuleName,
                        AccessRuleKey = AccessRuleKey,
                    },
                    EntityPaths =
                    {
                        TargetHostEcho,
                        TargetHostEcho,
                        TargetHostEcho,
                        TargetHostEcho33,
                        TargetHostEcho33
                    }
                }
            }
            .ToConfigurationArgs());

        private static readonly string[] ClientAgentArgs = QaDefaults
            .GetSerilogConfigurationArgs(ClientAgentName, QaDefaults.MakeDefaultLogPath(ClientAgentName))
            .MergeRange(new
            {
                AgentMetrics = new AgentMetricsOptions
                {
                    MetricsOptions =
                    {
                        DefaultContextLabel = "Azure Relay Port Bridge",
                        GlobalTags =
                        {
                            { "agent", "Client" }
                        },
                        Enabled = true,
                        ReportingEnabled = true
                    },
                    ReportingOptions =
                    {
                        ReportingFlushIntervalSeconds = 30,
                        ReportFile = Path.Combine(AppContext.BaseDirectory, "metrics-client.txt")
                    }
                }
            }.ToConfigurationArgs())
            .MergeRange(new
                {
                    PortBridge = new ClientAgentOptions
                    {
                        ServiceNamespace = new ServiceNamespaceOptions
                        {
                            ServiceNamespace = ServiceNamespace,
                            AccessRuleName = AccessRuleName,
                            AccessRuleKey = AccessRuleKey,
                        },
                        PortMappings = {
                        {
                            "5020", new PortMappingOptions
                            {
                                EntityPath = TargetHostEcho,
                                RemoteTcpPort = 5010,
                                AcceptFromIpAddresses =
                                {
                                    "127.0.0.1"
                                }
                            }
                        },
                        {
                            "5021", new PortMappingOptions
                            {
                                EntityPath = TargetHostEcho,
                                RemoteTcpPort = 5011,
                                AcceptFromIpAddresses =
                                {
                                    "127.0.0.1"
                                }
                            }
                        },
                        {
                            "5031", new PortMappingOptions
                            {
                                EntityPath = TargetHostEcho33,
                                RemoteTcpPort = 5011,
                                AcceptFromIpAddresses =
                                {
                                    "127.0.0.1"
                                }
                            }
                        }}
                    }
                }
                .ToConfigurationArgs());


        private IWebHost _echoServiceHost;
        private IWebHost _echoServiceHostWithClientCertificateRequirements;
        private Thread _serviceAgentThread;
        private Thread _clientAgentThread;

        [OneTimeSetUp]
        public async Task Setup()
        {
            Console.WriteLine("Setting up test environment...");

            StartEchoService();

            StartEchoServiceWithClientCertificateRequirements();

            StartServiceAgent();

            StartClientAgent();

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Console.WriteLine("Cleaning up test environment...");

            StopClientAgent();

            StopServiceAgent();

            StopEchoServiceWithClientCertificateRequirements();

            StopEchoService();
        }

        private void StartEchoServiceWithClientCertificateRequirements()
        {
            try
            {
                _echoServiceHostWithClientCertificateRequirements = WebHost.CreateDefaultBuilder()
                    .UseKestrel(o =>
                    {
                        var adapterOptions = new HttpsConnectionAdapterOptions
                        {
                            ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                            SslProtocols = SslProtocols.Tls12,
                            ServerCertificate = new X509Certificate2(Convert.FromBase64String(ServerCertificate), ServerCertificatePassword),
                            ClientCertificateValidation = (cert, chain, p) =>
                                string.Equals(cert.Thumbprint, ClientCertificateThumbprint, StringComparison.OrdinalIgnoreCase)
                        };

                        o.Listen(IPAddress.Any, 5010, l => l.UseHttps(adapterOptions));
                    })
                    .UseStartup<Startup>()
                    .Build();

                _echoServiceHostWithClientCertificateRequirements.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void StartEchoService()
        {
            try
            {
                _echoServiceHost = WebHost.CreateDefaultBuilder()
                    .UseUrls(EchoServiceBaseAddress)
                    .UseHttpSys()
                    .UseStartup<Startup>()
                    .Build();

                _echoServiceHost.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void StartServiceAgent()
        {
            _serviceAgentThread = new Thread(() => ServiceAgent.Console.Program.Main(ServiceAgentArgs))
            {
                IsBackground = true
            };

            _serviceAgentThread.Start();
        }

        private void StartClientAgent()
        {
            _clientAgentThread = new Thread(() => ClientAgent.Console.Program.Main(ClientAgentArgs))
            {
                IsBackground = true
            };

            _clientAgentThread.Start();
        }

        private void StopEchoServiceWithClientCertificateRequirements()
        {
            try
            {
                _echoServiceHostWithClientCertificateRequirements.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void StopEchoService()
        {
            try
            {
                _echoServiceHost.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void StopServiceAgent()
        {
            try
            {
                ServiceAgent.Console.Program.Blocker.Release();

                if (_serviceAgentThread == null)
                    return;

                if (!_serviceAgentThread.Join(TimeSpan.FromSeconds(5)))
                    _serviceAgentThread.Abort();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void StopClientAgent()
        {
            try
            {
                ClientAgent.Console.Program.Blocker.Release();

                if (_clientAgentThread == null)
                    return;

                if (!_clientAgentThread.Join(TimeSpan.FromSeconds(5)))
                    _clientAgentThread.Abort();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
