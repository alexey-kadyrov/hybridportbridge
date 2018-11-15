namespace DocaLabs.HybridPortBridge.Config
{
    public sealed class MetricsReportingOptions
    {
        public int ReportingFlushIntervalSeconds { get; set; } = -1;
        public bool ReportConsole { get; set; }
        public string ReportFile { get; set; }
    }
}