using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocaLabs.HybridPortBridge.Config;
using Microsoft.Azure.Relay;
using Newtonsoft.Json.Linq;
using Serilog;

namespace DocaLabs.HybridPortBridge.ServiceAgent
{
    public sealed class ServiceConnectionForwarder : IConnectionForwarder
    {
        private readonly ILogger _log;
        private readonly Uri _endpointVia;
        private readonly int _idx;
        private readonly string _entityPath;
        private readonly HybridConnectionListener _relayListener;
        private string _targetHost;
        private AllowedPorts _allowedPorts;
        private readonly ConcurrentDictionary<object, RelayConnection> _connections;

        public ServiceConnectionForwarder(ILogger loggerFactory, int idx, ServiceNamespaceOptions serviceNamespace, string entityPath)
        {
            _log = loggerFactory?.ForContext(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

            _idx = idx;
            _entityPath = entityPath;

            _connections = new ConcurrentDictionary<object, RelayConnection>();
            _endpointVia = new UriBuilder("sb", serviceNamespace.ServiceNamespace, -1, _entityPath).Uri;

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(serviceNamespace.AccessRuleName, serviceNamespace.AccessRuleKey);
            _relayListener = new HybridConnectionListener(_endpointVia, tokenProvider);

            ParseRelayMetadata();
        }

        public void Start()
        {
            _log.Information("Relay: {idx}:{relay}. Opening relay listener connection", _idx, _endpointVia);

            try
            {
                _relayListener.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

                _relayListener.AcceptConnectionAsync().ContinueWith(StreamAccepted);
            }
            catch (Exception e)
            {
                _log.Error(e, "Relay: {idx}:{relay}. Unable to connect", _idx, _endpointVia);
                throw;
            }
        }

        public void Stop()
        {
            _log.Information("Relay: {idx}:{relay}. Closing relay listener connection", _idx, _endpointVia);

            _connections.DisposeAndClear();

            _relayListener.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        private void ParseRelayMetadata()
        {
            var info = _relayListener.GetRuntimeInformationAsync(default(CancellationToken))
                .GetAwaiter()
                .GetResult();

            try
            {
                var userData = JArray.Parse(info.UserMetadata);

                var endpoint = userData.FirstOrDefault(x => x["key"].Value<string>() == "endpoint");
                if(endpoint == null)
                    throw new ConfigurationErrorException($"Expected endpoint key was not found in the {_endpointVia} relay user metadata");

                var targetHostInfo = endpoint["value"].Value<string>();
                if(string.IsNullOrWhiteSpace(targetHostInfo))
                    throw new ConfigurationErrorException($"The endpoint value is null or empty string in the {_endpointVia} relay user metadata");

                var parts = targetHostInfo.Split(new [] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length != 2)
                    throw new ConfigurationErrorException($"Wrong format of the endpoint {targetHostInfo} value in the {_endpointVia} relay user metadata");

                _targetHost = parts[0];

                _allowedPorts = new AllowedPorts(parts[1]);
            }
            catch (Exception e)
            {
                if (e is ConfigurationErrorException)
                    throw;

                throw new ConfigurationErrorException($"Failed to parse the {_endpointVia} relay user metadata", e);
            }
        }

        private async Task StreamAccepted(Task<HybridConnectionStream> prev)
        {
            try
            {
#pragma warning disable 4014
                _relayListener.AcceptConnectionAsync().ContinueWith(StreamAccepted);
#pragma warning restore 4014

                if (prev.Exception != null)
                    throw prev.Exception;

                var relayStream = prev.Result;
                    
                if (relayStream != null)
                {
                    var connectionInfo = await relayStream.ReadLengthPrefixedStringAsync();

                    if (connectionInfo.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!int.TryParse(connectionInfo.Substring(4), out var port))
                        {
                            CloseRelayStreamConnection(relayStream, "Bad target port format");
                            return;
                        }

                        if (!_allowedPorts.IsAllowed(port))
                        {
                            CloseRelayStreamConnection(relayStream, $"Incoming connection for port {port} not permitted");
                            return;
                        }

                        _log.Debug("Relay: {idx}:{relay}. Incoming connection for port {port}", _idx, _endpointVia, port);

                        var connection = CreateConnection(relayStream, port);

                        connection.Start();
                    }
                    else
                    {
                        CloseRelayStreamConnection(relayStream, $"Unable to handle connection for {connectionInfo}");
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Relay: {idx}:{relay}. Error accepting connection", _idx, _endpointVia);
            }
        }

        private RelayConnection CreateConnection(HybridConnectionStream relayStream, int port)
        {
            var connection = new RelayConnection(_log, _idx, _entityPath, relayStream, _targetHost, port, OnConnectionCompleted);

            _connections[connection] = connection;

            return connection;
        }

        private Task OnConnectionCompleted(RelayConnection obj)
        {
            if (_connections.TryRemove(obj, out var connection))
            {
                connection.IgnoreException(x => x.Dispose());
            }

            return Task.CompletedTask;
        }

        private void CloseRelayStreamConnection(Stream stream, string reason)
        {
            _log.Warning("Relay: {idx}:{relay}. " + reason, _idx, _endpointVia);

            stream.IgnoreException(x => x.Dispose());
        }
    }
}