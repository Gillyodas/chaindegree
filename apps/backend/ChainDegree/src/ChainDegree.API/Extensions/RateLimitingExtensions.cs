using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace ChainDegree.API.Extensions
{
    public static class RateLimitingExtensions
    {
        public const string VerifyDegreePolicy = "verify-degree";
        public const string IssueDegreePolicy = "issue-degree";
        public const string UpdateDegreePolicy = "update-degree";
        public const string RevokeDegreePolicy = "revoke-degree";
        public const string RetryDegreePolicy = "retry-degree";
        public const string GetBatchStatusPolicy = "get-batch-status";

        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Public verification endpoint rate limit (strict: 30 req/min)
                options.AddFixedWindowLimiter(VerifyDegreePolicy, config =>
                {
                    config.PermitLimit = 30;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });

                // Issue degrees batch endpoint rate limit (60 req/min)
                options.AddFixedWindowLimiter(IssueDegreePolicy, config =>
                {
                    config.PermitLimit = 60;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });

                // Update degree endpoint rate limit (30 req/min)
                options.AddFixedWindowLimiter(UpdateDegreePolicy, config =>
                {
                    config.PermitLimit = 30;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });

                // Revoke degree endpoint rate limit (30 req/min)
                options.AddFixedWindowLimiter(RevokeDegreePolicy, config =>
                {
                    config.PermitLimit = 30;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });

                // Retry confirmation endpoint rate limit (30 req/min)
                options.AddFixedWindowLimiter(RetryDegreePolicy, config =>
                {
                    config.PermitLimit = 30;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });

                // Get batch status polling endpoint rate limit (120 req/min)
                options.AddFixedWindowLimiter(GetBatchStatusPolicy, config =>
                {
                    config.PermitLimit = 120;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });
            });

            return services;
        }
    }
}
