using System;
using System.IO;
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
        public const string EntityPath = "ovc-cicd-test-sql";

        private const string ClientAgentName = "PostgeSQL.ClientAgent";
        private const string ServiceAgentName = "PostgeSQL.MySqlServiceAgent";

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
                            "5041", new PortMappingOptions
                            {
                                EntityPath = EntityPath,
                                RemoteTcpPort = 5432,
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


        private AgentHost _serviceAgent;
        private AgentHost _clientAgent;

        [OneTimeSetUp]
        public async Task Setup()
        {
            try
            {
                Console.WriteLine("Setting up test environment...");
    
                StartServiceAgent();
    
                StartClientAgent();
    
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            Console.WriteLine("Cleaning up test environment...");

            StopClientAgent();

            StopServiceAgent();
        }

        private void StartServiceAgent()
        {
            _serviceAgent = DocaLabs.HybridPortBridge.ServiceAgent.Console.ServiceForwarderHost.Build(ServiceAgentArgs);

            _serviceAgent.Start();
        }

        private void StartClientAgent()
        {
            _clientAgent = DocaLabs.HybridPortBridge.ClientAgent.Console.ClientForwarderHost.Build(ClientAgentArgs);

            _clientAgent.Start();
        }

        private void StopServiceAgent()
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

        private void StopClientAgent()
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
