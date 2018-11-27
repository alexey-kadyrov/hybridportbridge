using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;

namespace DocaLabs.HybridPortBridge.Metrics
{
    public sealed class ReportScheduler
    {
        private readonly IMetricsRoot _metrics;
        private readonly Timer _timer;

        public ReportScheduler(IMetricsRoot metrics, TimeSpan period)
        {
            _metrics = metrics;
            _timer = new Timer(Report, null, period, period);
        }

        public void Stop()
        {
            _timer.Dispose();
            ReportAsync().GetAwaiter().GetResult();
        }

        private void Report(object state)
        {
            ReportAsync().GetAwaiter().GetResult();
        }

        private Task ReportAsync()
        {
            return Task.WhenAll(_metrics.ReportRunner.RunAllAsync());
        }
    }
}
