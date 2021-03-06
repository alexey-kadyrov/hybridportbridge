﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using DocaLabs.Qa;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Resilience.IntegrationTests.Client;

namespace Resilience.IntegrationTests
{
    [SetUpFixture]
    public class TestEnvironmentSetup
    {
        // port mapping 5021 to 5011
        public const string ServiceBaseAddressForClientAgent = "http://localhost:5021";
        private const string ServiceBaseAddress = "http://localhost:5011/";

        private const string EntityPath = "simple";

        private const string ClientAgentName = "ClientAgent";
        private const string ServiceAgentName = "ServiceAgent";

        private static readonly string[] ServiceAgentArgs = QaDefaults
            .GetSerilogConfigurationArgs(ServiceAgentName, QaDefaults.MakeDefaultLogPath(ServiceAgentName))
            .MergeConfigurationArgs(new
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
                },

                PortBridge = new ServiceAgentOptions
                {
                    EntityPaths =
                    {
                        EntityPath
                    }
                }
            });

        private static readonly string[] ClientAgentArgs = QaDefaults
            .GetSerilogConfigurationArgs(ClientAgentName, QaDefaults.MakeDefaultLogPath(ClientAgentName))
            .MergeConfigurationArgs(new
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
                },

                PortBridge = new ClientAgentOptions
                {
                    PortMappings =
                    {
                        {
                            "5021", new PortMappingOptions
                            {
                                EntityPath = EntityPath,
                                RemoteConfigurationKey = 5011,
                                RelayChannelCount = 2,
                                AcceptFromIpAddresses =
                                {
                                    "127.0.0.1"
                                }
                            }
                        }
                    }
                }
            });


        private IWebHost _serviceHost;

        private static ConsoleAgentHost _serviceAgent;
        private static Thread _serviceAgentThread;

        private static ConsoleAgentHost _clientAgent;
        private static Thread _clientAgentThread;

        [OneTimeSetUp]
        public async Task Setup()
        {
            try
            {
                TestContext.WriteLine("Setting up test environment...");

                ServiceLocator.Initialize((services, configuration) =>
                {
                    services.AddScoped(sp => ClientFactory.CreateRequest());
                });

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

            StopClientAgent();

            StopServiceAgent();

            await StopService();
        }

        public async Task StartService()
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

        public static void StartServiceAgent()
        {
            _serviceAgentThread = new Thread(() =>
            {
                _serviceAgent = DocaLabs.HybridPortBridge.ServiceAgent.ServiceForwarderHost.Build(ServiceAgentArgs);
                _serviceAgent.Start();
            })
            {
                IsBackground = true
            };

            _serviceAgentThread.Start();
        }

        public static void StartClientAgent()
        {
            _clientAgentThread = new Thread(() =>
            {
                _clientAgent = DocaLabs.HybridPortBridge.ClientAgent.ClientForwarderHost.Build(ClientAgentArgs);
                _clientAgent.Start();
            })
            {
                IsBackground = true
            };

            _clientAgentThread.Start();
        }

        private async Task StopService()
        {
            try
            {
                await _serviceHost
                    .StopAsync(new  CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
            }
        }

        public static void StopServiceAgent()
        {
            try
            {
                _serviceAgent.Stop();

                if (!_serviceAgentThread.Join(TimeSpan.FromSeconds(5)))
                    _serviceAgentThread.Abort();
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
            }
        }

        public static void StopClientAgent()
        {
            try
            {
                _clientAgent.Stop();

                if (!_clientAgentThread.Join(TimeSpan.FromSeconds(5)))
                    _clientAgentThread.Abort();
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
            }
        }
    }
}
