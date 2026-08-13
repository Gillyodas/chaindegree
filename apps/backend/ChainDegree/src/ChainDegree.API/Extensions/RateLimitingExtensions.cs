using System;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace ChainDegree.API.Extensions
{
    public static class RateLimitPolicies
    {
        public static class Degrees
        {
            public const string Verify = "degrees:verify";
            public const string Issue = "degrees:issue";
            public const string Update = "degrees:update";
            public const string Revoke = "degrees:revoke";
            public const string Retry = "degrees:retry";
            public const string BatchStatus = "degrees:batch-status";
            public const string Read = "degrees:read";
        }

        public static class Students
        {
            public const string Search = "students:search";
            public const string Read = "students:read";
        }

        public static class Recruiters
        {
            public const string Search = "recruiters:search";
            public const string Read = "recruiters:read";
        }

        public static class Jobs
        {
            public const string Read = "jobs:read";
            public const string Write = "jobs:write";
        }

        public static class Reports
        {
            public const string Submit = "reports:submit";
            public const string Review = "reports:review";
            public const string Export = "reports:export";
        }
    }

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // ========================================================
                // DOMAIN MODULE 1: DEGREES (Văn bằng & Xác thực)
                // ========================================================
                ConfigureDegreePolicies(options);

                // ========================================================
                // DOMAIN MODULE 2: STUDENTS (Sinh viên)
                // ========================================================
                ConfigureStudentPolicies(options);

                // ========================================================
                // DOMAIN MODULE 3: RECRUITERS & JOBS
                // ========================================================
                ConfigureRecruiterAndJobPolicies(options);

                // ========================================================
                // DOMAIN MODULE 4: REPORTS & COMPLAINTS
                // ========================================================
                ConfigureReportPolicies(options);
            });

            return services;
        }

        private static void ConfigureDegreePolicies(RateLimiterOptions options)
        {
            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Verify, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Issue, config =>
            {
                config.PermitLimit = 60;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Update, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Revoke, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Retry, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.BatchStatus, config =>
            {
                config.PermitLimit = 120;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Read, config =>
            {
                config.PermitLimit = 120;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });
        }

        private static void ConfigureStudentPolicies(RateLimiterOptions options)
        {
            options.AddFixedWindowLimiter(RateLimitPolicies.Students.Search, config =>
            {
                config.PermitLimit = 60;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Students.Read, config =>
            {
                config.PermitLimit = 120;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });
        }

        private static void ConfigureRecruiterAndJobPolicies(RateLimiterOptions options)
        {
            options.AddFixedWindowLimiter(RateLimitPolicies.Recruiters.Search, config =>
            {
                config.PermitLimit = 60;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Jobs.Read, config =>
            {
                config.PermitLimit = 120;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter(RateLimitPolicies.Jobs.Write, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });
        }

        private static void ConfigureReportPolicies(RateLimiterOptions options)
        {
            // Report submission rate limiter partitioned by IP + UserID
            options.AddPolicy(RateLimitPolicies.Reports.Submit, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{ip}:{userId}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            // Report review endpoint (30 req/min)
            options.AddFixedWindowLimiter(RateLimitPolicies.Reports.Review, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });
        }
    }
}
