using System.Collections.Generic;
using System.Linq;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class AllowedPorts
    {
        private readonly List<int> _allowedPorts = new List<int>();
        private readonly bool _noPortConstraints;

        public AllowedPorts(string allowedPorts)
        {
            var allowedPortsString = allowedPorts.Trim();

            if (allowedPortsString == "*")
            {
                _noPortConstraints = true;
            }
            else
            {
                _noPortConstraints = false;

                var portList = allowedPortsString.Split(',');

                _allowedPorts.AddRange(portList.Select(port => int.Parse(port.Trim())));
            }
        }

        public bool IsAllowed(int port)
        {
            return _noPortConstraints || _allowedPorts.Any(p => p == port);
        }
    }
}
