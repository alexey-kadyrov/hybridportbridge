﻿using System;
using System.IO;
using System.Net;
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
        public const string ServiceBaseAddress = "http://localhost:5011/";

        public const string EntityPath = "ovc-cicd-relay-echo";

        private const string ClientAgentName = "ClientAgent";
        private const string ServiceAgentName = "ServiceAgent";

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
                    EntityPaths =
                    {
                        EntityPath
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
                    PortMappings =
                    {
                        {
                            "5021", new PortMappingOptions
                            {
                                EntityPath = EntityPath,
                                RemoteTcpPort = 5011,
                                AcceptFromIpAddresses =
                                {
                                    "127.0.0.1"
                                }
                            }
                        }
                    }
                }
            }
            .ToConfigurationArgs());


        private IWebHost _serviceHost;
        private static AgentHost _serviceAgent;
        private static AgentHost _clientAgent;

        [OneTimeSetUp]
        public async Task Setup()
        {
            Console.WriteLine("Setting up test environment...");

            ServicePointManager.FindServicePoint(ServiceBaseAddressForClientAgent, null).ConnectionLeaseTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

            ServiceLocator.Initialize((services, configuration) =>
            {
                services.AddScoped(sp => ClientFactory.CreateRequest());
            });

            await StartService();
        }

        [OneTimeTearDown]
        public async Task Cleanup()
        {
            Console.WriteLine("Cleaning up test environment...");

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
                Console.WriteLine(e);
            }
        }

        public static void StartServiceAgent()
        {
            _serviceAgent = DocaLabs.HybridPortBridge.ServiceAgent.Console.PortBridgeServiceForwarderHost.Configure(ServiceAgentArgs);

            _serviceAgent.Start();
        }

        public static void StartClientAgent()
        {
            _clientAgent = DocaLabs.HybridPortBridge.ClientAgent.Console.PortBridgeClientForwarderHost.Configure(ClientAgentArgs);

            _clientAgent.Start();
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
                Console.WriteLine(e);
            }
        }

        public static void StopServiceAgent()
        {
            try
            {
                _serviceAgent?.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void StopClientAgent()
        {
            try
            {
                _clientAgent?.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
