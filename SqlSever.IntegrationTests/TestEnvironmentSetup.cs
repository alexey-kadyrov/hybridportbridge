using System;
using System.IO;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.Hosting;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using DocaLabs.Qa;
using NUnit.Framework;

namespace SqlSever.IntegrationTests
{
    [SetUpFixture]
    public class TestEnvironmentSetup
    {
        private const string EntityPath = "ovc-cicd-test-sql";
        private const string ClientAgentName = "SQLServer.ClientAgent";
        private const string ServiceAgentName = "SQLServer.MySqlServiceAgent";

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
                }
            })
            .MergeConfigurationArgs(new
            {
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
                }
            })
            .MergeConfigurationArgs(new
            {
                PortBridge = new ClientAgentOptions
                {
                    PortMappings =
                    {
                        {
                            "14334", new PortMappingOptions
                            {
                                EntityPath = EntityPath,
                                RemoteConfigurationKey = 14333,
                                AcceptFromIpAddresses =
                                {
                                    "0.0.0.0-255.255.255.255"
                                }
                            }
                        }
                    }
                }
            });


        private ConsoleAgentHost _serviceConsoleAgent;
        private ConsoleAgentHost _clientAgent;

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
            _serviceConsoleAgent = DocaLabs.HybridPortBridge.ServiceAgent.ServiceForwarderHost.Build(ServiceAgentArgs);

            _serviceConsoleAgent.Start();
        }

        private void StartClientAgent()
        {
            _clientAgent = DocaLabs.HybridPortBridge.ClientAgent.ClientForwarderHost.Build(ClientAgentArgs);

            _clientAgent.Start();
        }

        private void StopServiceAgent()
        {
            try
            {
                _serviceConsoleAgent?.Stop();
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
                _clientAgent?.Stop();
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
            }
        }
    }
}
