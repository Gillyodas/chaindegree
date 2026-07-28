using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace ChainDegree.API.Extensions
{
    public static class RateLimitingExtensions
    {
        public const string VerifyDegreePolicy = "verify-degree";

        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter(VerifyDegreePolicy, config =>
                {
                    config.PermitLimit = 30;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });
            });

            return services;
        }
    }
}
