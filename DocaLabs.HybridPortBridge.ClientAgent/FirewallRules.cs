using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DocaLabs.HybridPortBridge.ClientAgent.Config;
using DocaLabs.HybridPortBridge.Config;

namespace DocaLabs.HybridPortBridge.ClientAgent
{
    internal sealed class FirewallRules
    {
        private readonly ICollection<IPRange> _firewallRules;
        private bool _acceptAny;

        public FirewallRules(PortMappingOptions portMappings)
        {
            _firewallRules = new List<IPRange>();
            BuildIPRange(portMappings.AcceptFromIpAddresses);
        }

        public bool IsInRange(IPEndPoint remoteIPEndpoint)
        {
            return _acceptAny || _firewallRules.Any(range => range.IsInRange(remoteIPEndpoint.Address));
        }

        private void BuildIPRange(IReadOnlyCollection<string> ranges)
        {
            if (ranges == null)
                return;

            try
            {
                foreach (var rule in ranges)
                {
                    if(string.IsNullOrWhiteSpace(rule))
                        continue;

                    if (rule == "*")
                    {
                        _firewallRules.Clear();
                        _acceptAny = true;
                        return;
                    }

                    var parts = rule.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);

                    switch (parts.Length)
                    {
                        case 1:
                            _firewallRules.Add(new IPRange(IPAddress.Parse(parts[0])));
                            break;
                        case 2:
                            _firewallRules.Add(new IPRange(IPAddress.Parse(parts[0]), IPAddress.Parse(parts[1])));
                            break;
                        default:
                            _firewallRules.Clear();
                            throw new ConfigurationErrorException("The IP range must be either single IP address or two addresses separated by the dash (-) like 10.1.34.01-10.1.34.255");
                    }
                }
            }
            catch (Exception e) when(!(e is ConfigurationErrorException))
            {
                _firewallRules.Clear();
                throw new ConfigurationErrorException("Bad firewall rules", e);
            }
        }
    }
}
