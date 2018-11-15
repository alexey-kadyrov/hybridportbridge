using System;
using Microsoft.AspNetCore.Http;

namespace DocaLabs.HybridPortBridge.IntegrationTests.Service
{
    public static class HttpRequestExtensions
    {
        public static bool IsGetPath(this HttpRequest request, string path)
        {
            return string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(request.Path, path, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPostPath(this HttpRequest request, string path)
        {
            return string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(request.Path, path, StringComparison.OrdinalIgnoreCase);
        }
    }
}