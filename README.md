# Azure Hybrid Connection Relay Port Bridge

"Hybrid Port Bridge" is a point-to-point tunneling utility that allows mapping
TCP listener ports from a machine on network A to another machine on a different
network B, and make it appear as if the listener were local on network B.

The utility is based on the original sample from Microsoft found here:
https://github.com/Azure/azure-relay/tree/master/samples/hybrid-connections/dotnet/portbridge

Hybrid Port Bridge is similar to what can generally be achieved via SSH tunneling, but
is realized over the Relay so that both parties can reside safely behind Firewalls,
leverage the Service Bus authorization integration, and have all communication run
over the Firewall-friendly WebSocket protocol over port 443, see
https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-hybrid-connections-protocol#protocol-reference

For more details see https://github.com/alexey-kadyrov/hybridportbridge/wiki