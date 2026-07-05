using System.Collections.Generic;

namespace ChainDegree.API.Filters
{
    public static class ErrorTypeMap
    {
        private static readonly Dictionary<int, string> Slugs = new Dictionary<int, string>
        {
            { 400, "bad-request" },
            { 401, "unauthorized" },
            { 403, "forbidden" },
            { 404, "not-found" },
            { 409, "conflict" },
            { 500, "internal-server-error" }
        };

        public static string GetErrorUri(int statusCode)
        {
            if (Slugs.TryGetValue(statusCode, out var slug))
            {
                return $"https://chaindegree.io/errors/{slug}";
            }
            return $"https://chaindegree.io/errors/unexpected-error";
        }
    }
}
