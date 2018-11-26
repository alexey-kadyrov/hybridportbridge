using App.Metrics;
using App.Metrics.Meter;

namespace DocaLabs.HybridPortBridge.Metrics
 {
     public sealed class TunnelMetrics
     {
         private readonly MetricTags _tags;

         private static readonly MeterOptions LocalEstablishedConnectionsOptions = new MeterOptions
         {
             Name = "Established Connections (Local)",
             MeasurementUnit = Unit.Items
         };

         private static readonly MeterOptions RemoteEstablishedTunnelsOptions = new MeterOptions
         {
             Name = "Established Tunnels (Remote)",
             MeasurementUnit = Unit.Items
         };
         
         public LocalDataChannelMetrics Local { get; }
         public RemoteDataChannelMetrics Remote { get; }
         public MeterMetric LocalEstablishedConnections { get; }
         public MeterMetric RemoteEstablishedTunnels { get; }
 
         public TunnelMetrics(MetricsRegistry registry, MetricTags tags)
         {
             _tags = tags;
             var metrics = registry.Merge(tags);
             
             Local = new LocalDataChannelMetrics(metrics);
             Remote = new RemoteDataChannelMetrics(metrics);
             LocalEstablishedConnections = metrics.MakeMeter(LocalEstablishedConnectionsOptions);
             RemoteEstablishedTunnels = metrics.MakeMeter(RemoteEstablishedTunnelsOptions);
         }

         public override string ToString()
         {
             return _tags.AsString();
         }
     }
 }