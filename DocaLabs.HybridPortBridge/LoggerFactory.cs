﻿using Microsoft.Extensions.Configuration;
using Serilog;

namespace DocaLabs.HybridPortBridge
{
    public static class LoggerFactory
    {
        public static ILogger Initialize(IConfiguration configuration)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
        }
    }
}
