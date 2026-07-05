using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace ChainDegree.API.Filters
{
    public class ChainDegreeProblemDetailsFactory : ProblemDetailsFactory
    {
        private readonly ApiBehaviorOptions _options;

        public ChainDegreeProblemDetailsFactory(IOptions<ApiBehaviorOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            statusCode ??= 500;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance ?? httpContext?.Request?.Path
            };

            if (_options.ClientErrorMapping.TryGetValue(statusCode.Value, out var clientErrorData))
            {
                problemDetails.Title ??= clientErrorData.Title;
                problemDetails.Type ??= clientErrorData.Link;
            }

            // Standardize types with internal URLs
            if (problemDetails.Type == null || problemDetails.Type.StartsWith("https://tools.ietf.org/html/rfc"))
            {
                problemDetails.Type = ErrorTypeMap.GetErrorUri(statusCode.Value);
            }

            EnrichProblemDetails(httpContext!, problemDetails);

            return problemDetails;
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            if (modelStateDictionary == null)
            {
                throw new ArgumentNullException(nameof(modelStateDictionary));
            }

            statusCode ??= 400;

            var problemDetails = new ValidationProblemDetails(modelStateDictionary)
            {
                Status = statusCode,
                Title = title ?? "Validation Error",
                Type = type ?? ErrorTypeMap.GetErrorUri(statusCode.Value),
                Detail = detail ?? "One or more validation errors occurred.",
                Instance = instance ?? httpContext?.Request?.Path
            };

            EnrichProblemDetails(httpContext!, problemDetails);

            // Validation specific error code
            if (!problemDetails.Extensions.ContainsKey("errorCode"))
            {
                problemDetails.Extensions["errorCode"] = "VALIDATION_FAILED";
            }

            return problemDetails;
        }

        private void EnrichProblemDetails(HttpContext? httpContext, ProblemDetails problemDetails)
        {
            if (httpContext == null) return;

            // Timestamp
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("o");

            // Trace Identifier (traceId)
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            if (!string.IsNullOrEmpty(traceId))
            {
                problemDetails.Extensions["traceId"] = traceId;
            }

            // Correlation ID (requestId) from X-Request-Id or X-Correlation-Id
            string? correlationId = null;
            if (httpContext.Request.Headers.TryGetValue("X-Request-Id", out var reqIdHeader))
            {
                correlationId = reqIdHeader.ToString();
            }
            else if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var corrIdHeader))
            {
                correlationId = corrIdHeader.ToString();
            }

            if (!string.IsNullOrEmpty(correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId;
            }
        }
    }
}
