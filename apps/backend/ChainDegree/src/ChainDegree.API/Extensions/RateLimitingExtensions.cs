using System;
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
                // DOMAIN MODULE 2: STUDENTS (Sinh viên) — Extensible
                // ========================================================
                ConfigureStudentPolicies(options);

                // ========================================================
                // DOMAIN MODULE 3: RECRUITERS & JOBS — Extensible
                // ========================================================
                ConfigureRecruiterAndJobPolicies(options);
            });

            return services;
        }

        private static void ConfigureDegreePolicies(RateLimiterOptions options)
        {
            // Public verification endpoint rate limit (strict: 30 req/min)
            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Verify, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            // Issue degrees batch endpoint rate limit (60 req/min)
            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Issue, config =>
            {
                config.PermitLimit = 60;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            // Update degree endpoint rate limit (30 req/min)
            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Update, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            // Revoke degree endpoint rate limit (30 req/min)
            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Revoke, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            // Retry confirmation endpoint rate limit (30 req/min)
            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.Retry, config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueLimit = 0;
            });

            // Get batch status polling endpoint rate limit (120 req/min)
            options.AddFixedWindowLimiter(RateLimitPolicies.Degrees.BatchStatus, config =>
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
    }
}
