using System;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Relay;
using Newtonsoft.Json.Linq;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class RelayMetadata
    {
        public string TargetHost { get; }
        public AllowedPorts AllowedPorts { get; }

        private RelayMetadata(string targetHost, AllowedPorts allowedPorts)
        {
            TargetHost = targetHost;
            AllowedPorts = allowedPorts;
        }

        public bool IsPortAllowed(int port)
        {
            return AllowedPorts.IsAllowed(port);
        }

        public static RelayMetadata Parse(HybridConnectionListener listener)
        {
            var info = listener.GetRuntimeInformationAsync(default(CancellationToken))
                .GetAwaiter()
                .GetResult();

            try
            {
                var userData = JArray.Parse(info.UserMetadata);

                var endpoint = userData.FirstOrDefault(x => x["key"].Value<string>() == "endpoint");
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
            catch (Exception e)
            {
                if (e is ConfigurationErrorException)
                    throw;

                throw new ConfigurationErrorException($"Failed to parse the {listener.Address} relay user metadata", e);
            }
        }
    }
}
