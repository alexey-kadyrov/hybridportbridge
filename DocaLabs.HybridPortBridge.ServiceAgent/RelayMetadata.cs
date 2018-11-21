using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using Newtonsoft.Json.Linq;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class RelayMetadata
    {
        private readonly AllowedPorts _allowedPorts;
        public string TargetHost { get; }

        private RelayMetadata(string targetHost, AllowedPorts allowedPorts)
        {
            TargetHost = targetHost;
            _allowedPorts = allowedPorts;
        }

        public bool IsPortAllowed(int port)
        {
            return _allowedPorts.IsAllowed(port);
        }

        public static async Task<RelayMetadata> Parse(HybridConnectionListener listener)
        {
            var info = await listener.GetRuntimeInformationAsync(default(CancellationToken));

            try
            {
                var metadata = JArray.Parse(info.UserMetadata);

                var endpoint = metadata.FirstOrDefault(x => x["key"].Value<string>() == "endpoint");
                if (endpoint == null)
                    throw new ConfigurationErrorException($"Expected endpoint key was not found in the {listener.Address} relay user metadata");

                var targetHostInfo = endpoint["value"].Value<string>();
                if (string.IsNullOrWhiteSpace(targetHostInfo))
                    throw new ConfigurationErrorException($"The endpoint value is null or empty string in the {listener.Address} relay user metadata");

                var parts = targetHostInfo.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new ConfigurationErrorException($"Wrong format of the endpoint {targetHostInfo} value in the {listener.Address} relay user metadata");

                return new RelayMetadata(parts[0], new AllowedPorts(parts[1]));
            }
            catch (Exception e) when(!(e is ConfigurationErrorException))
            {
                throw new ConfigurationErrorException($"Failed to parse the {listener.Address} relay user metadata", e);
            }
        }
    }
}
