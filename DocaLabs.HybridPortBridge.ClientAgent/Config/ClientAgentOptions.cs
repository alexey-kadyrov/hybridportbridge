using System.Collections.Generic;
using DocaLabs.HybridPortBridge.Config;

namespace DocaLabs.HybridPortBridge.ClientAgent.Config
{
    public sealed class ClientAgentOptions
    {
        public ServiceNamespaceOptions ServiceNamespace { get; set; }

        public Dictionary<string, PortMappingOptions> PortMappings { get; } = new Dictionary<string, PortMappingOptions>();
    }
}