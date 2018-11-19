using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;
using DocaLabs.HybridPortBridge.ServiceAgent.Config;
using DocaLabs.Qa;
using NUnit.Framework;

namespace MySql.IntegrationTests
{
    [SetUpFixture]
    public class TestEnvironmentSetup
    {
        public const string EntityPath = "ovc-cicd-test-sql";

        private const string ClientAgentName = "MySqlClientAgent";
        private const string ServiceAgentName = "MySqlServiceAgent";

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
                            "3305", new PortMappingOptions
                            {
                                EntityPath = EntityPath,
                                RemoteTcpPort = 3306,
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


        private Thread _serviceAgentThread;
        private Thread _clientAgentThread;

        [OneTimeSetUp]
        public async Task Setup()
        {
            Console.WriteLine("Setting up test environment...");

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
        }

        private void StartServiceAgent()
        {
            _serviceAgentThread = new Thread(() => DocaLabs.HybridPortBridge.ServiceAgent.Console.Program.Main(ServiceAgentArgs))
            {
                IsBackground = true
            };

            _serviceAgentThread.Start();
        }

        private void StartClientAgent()
        {
            _clientAgentThread = new Thread(() => DocaLabs.HybridPortBridge.ClientAgent.Console.Program.Main(ClientAgentArgs))
            {
                IsBackground = true
            };

            _clientAgentThread.Start();
        }

        private void StopServiceAgent()
        {
            try
            {
                DocaLabs.HybridPortBridge.ServiceAgent.Console.Program.Blocker.Release();

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
                DocaLabs.HybridPortBridge.ClientAgent.Console.Program.Blocker.Release();

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
