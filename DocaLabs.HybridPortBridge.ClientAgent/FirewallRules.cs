using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DocaLabs.HybridPortBridge.ClientAgent.Config;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class FirewallRules
    {
        private readonly ICollection<IPRange> _firewallRules;

        public FirewallRules(PortMappingOptions portMappings)
        {
            _firewallRules = BuildIPRange(portMappings.AcceptFromIpAddresses);
        }

        public bool IsInRange(IPEndPoint remoteIPEndpoint)
        {
            return _firewallRules.Any(range => range.IsInRange(remoteIPEndpoint.Address));
        }

        private static ICollection<IPRange> BuildIPRange(IReadOnlyCollection<string> ranges)
        {
            var rules = new List<IPRange>();

            if (ranges == null)
                return rules;

            try
            {
                foreach (var rule in ranges)
                {
                    if(string.IsNullOrWhiteSpace(rule))
                        continue;

                    var parts = rule.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);

                    switch (parts.Length)
                    {
                        case 1:
                            rules.Add(new IPRange(IPAddress.Parse(parts[0])));
                            break;
                        case 2:
                            rules.Add(new IPRange(IPAddress.Parse(parts[0]), IPAddress.Parse(parts[1])));
                            break;
                        default:
                            throw new ConfigurationErrorException("The IP range must be either single IP address or two addresses separated by the dash (-) like 10.1.34.01-10.1.34.255");
                    }
                }

                return rules;
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorException("Bad firewall rules", e);
            }
        }
    }
}
