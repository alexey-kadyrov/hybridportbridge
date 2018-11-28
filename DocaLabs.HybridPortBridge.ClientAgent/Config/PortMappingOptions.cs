using System.Collections.Generic;

namespace DocaLabs.HybridPortBridge.ClientAgent.Config
{
    public sealed class PortMappingOptions
    {
        public string EntityPath { get; set; }
        public int RemoteConfigurationKey { get; set; }
        public string BindToAddress { get; set; }
        public List<string> AcceptFromIpAddresses { get; } = new List<string>();
        public int RelayChannelCount { get; set; } = 1;
        public int RelayConnectionTtlSeconds { get; set; } = 300;
    }
}