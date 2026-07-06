using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ChainDegree.API.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeaderKey = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            context.Items[CorrelationIdHeaderKey] = correlationId.ToString();

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeaderKey] = new[] { correlationId.ToString() };
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId.ToString()
            }))
            {
                await _next(context);
            }
        }
    }
}
