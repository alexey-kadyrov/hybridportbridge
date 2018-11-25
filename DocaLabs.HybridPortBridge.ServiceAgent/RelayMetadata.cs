using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using DocaLabs.HybridPortBridge.Config;
using Microsoft.Azure.Relay;
using Newtonsoft.Json.Linq;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    internal sealed class RelayMetadata
    {
        private readonly Dictionary<int, ILocalDataChannelFactory> _channelFactories;

        private RelayMetadata(Dictionary<int, ILocalDataChannelFactory> channelFactories)
        {
            _channelFactories = channelFactories;
        }

        public ILocalDataChannelFactory GetLocalDataChannelFactory(int configurationKey)
        {
            return _channelFactories.TryGetValue(configurationKey, out var factory)
                ? factory
                : null;
        }

        public static async Task<RelayMetadata> Parse(HybridConnectionListener listener)
        {
            var info = await listener.GetRuntimeInformationAsync(default(CancellationToken));

            try
            {
                var channelFactories = new Dictionary<int, ILocalDataChannelFactory>();

                var metadata = JArray.Parse(info.UserMetadata);

                foreach (var item in metadata)
                {
                    var key = item["key"].Value<string>();
                    if(!int.TryParse(key, out var configurationKey))
                        continue;

                    var factory = ParseEndpoint(configurationKey, item["value"].Value<string>(), listener.Address);
                    if (factory != null)
                        channelFactories[configurationKey] = factory;
                }

                if(!channelFactories.Any())
                    throw new ConfigurationErrorException($"There is no any endpoint configured for {listener.Address}");

                return new RelayMetadata(channelFactories);
            }
            catch (Exception e) when(!(e is ConfigurationErrorException))
            {
                throw new ConfigurationErrorException($"Failed to parse the {listener.Address} relay user metadata", e);
            }
        }

        private static ILocalDataChannelFactory ParseEndpoint(int configurationKey, string endpoint, Uri listenerAddress)
        {
            var parts = endpoint.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                throw new ConfigurationErrorException($"Wrong format of the endpoint {endpoint} value in the {listenerAddress} relay user metadata");

            if(!string.Equals(parts[0], "tcp"))
                throw new ConfigurationErrorException($"Unsupported protocol {parts[0]} for the endpoint {endpoint} value in the {listenerAddress} relay user metadata");

            if(!int.TryParse(parts[2], out var port))
                throw new ConfigurationErrorException($"Wrong port format {parts[2]} for the endpoint {endpoint} value in the {listenerAddress} relay user metadata");

            return new LocalTcpDataChannelFactory(new MetricTags(nameof(configurationKey), configurationKey.ToString()),  parts[1], port);
        }
    }
}
