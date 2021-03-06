﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using DocaLabs.Qa;
using NUnit.Framework;

namespace PostgreSQL.IntegrationTests
{
    [SetUpFixture]
    public class TestEnvironmentSetup
    {
        private const string EntityPath = "sql";

        private const string ClientAgentName = "PostgeSQL.ClientAgent";
        private const string ServiceAgentName = "PostgeSQL.MySqlServiceAgent";

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
                            "5041", new PortMappingOptions
                            {
                                EntityPath = EntityPath,
                                RemoteConfigurationKey = 5432,
                                AcceptFromIpAddresses =
                                {
                                    "127.0.0.1"
                                }
                            }
                        }
                    }
                }
            });


        private ConsoleAgentHost _serviceAgent;
        private Thread _serviceAgentThread;

        private ConsoleAgentHost _clientAgent;
        private Thread _clientAgentThread;

        [OneTimeSetUp]
        public async Task Setup()
        {
            try
            {
                TestContext.WriteLine("Setting up test environment...");
    
                StartServiceAgent();
    
                StartClientAgent();
    
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
                throw;
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            TestContext.WriteLine("Cleaning up test environment...");

            StopClientAgent();

            StopServiceAgent();
        }

        private void StartServiceAgent()
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

        private void StartClientAgent()
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

        private void StopServiceAgent()
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

        private void StopClientAgent()
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
