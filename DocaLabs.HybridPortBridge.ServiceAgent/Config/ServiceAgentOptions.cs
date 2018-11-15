using System.Collections.Generic;
using DocaLabs.HybridPortBridge.Config;

namespace DocaLabs.HybridPortBridge.ServiceAgent.Config
{
    public class ServiceAgentOptions
    {
        public ServiceNamespaceOptions ServiceNamespace { get; set; }

        public List<string> EntityPaths { get; } = new List<string>();
    }
}