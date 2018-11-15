using Microsoft.Azure.Relay;

namespace DocaLabs.HybridPortBridge.Config
{
    public sealed class ServiceNamespaceOptions
    {
        public string ServiceNamespace { get; set; }
        public string AccessRuleName { get; set; }
        public string AccessRuleKey { get; set; }

        public TokenProvider CreateSasTokenProvider()
        {
            return TokenProvider.CreateSharedAccessSignatureTokenProvider(AccessRuleName, AccessRuleKey);
        }
    }
}