using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace ChainDegree.API.Filters
{
    public static class ErrorTypeMap
    {
        private static readonly Dictionary<int, (string Slug, string ErrorCode)> Mappings = new Dictionary<int, (string, string)>
        {
            { StatusCodes.Status400BadRequest, ("bad-request", "BAD_REQUEST") },
            { StatusCodes.Status401Unauthorized, ("unauthorized", "UNAUTHORIZED") },
            { StatusCodes.Status403Forbidden, ("forbidden", "FORBIDDEN") },
            { StatusCodes.Status404NotFound, ("not-found", "NOT_FOUND") },
            { StatusCodes.Status409Conflict, ("conflict", "CONFLICT") },
            { StatusCodes.Status500InternalServerError, ("internal-server-error", "INTERNAL_SERVER_ERROR") }
        };

        public static string GetErrorUri(int statusCode)
        {
            if (Mappings.TryGetValue(statusCode, out var map))
            {
                return $"https://chaindegree.io/errors/{map.Slug}";
            }
            return $"https://chaindegree.io/errors/unexpected-error";
        }

        public static string GetDefaultErrorCode(int statusCode)
        {
            if (Mappings.TryGetValue(statusCode, out var map))
            {
                return map.ErrorCode;
            }
            return "UNEXPECTED_ERROR";
        }
    }
}
