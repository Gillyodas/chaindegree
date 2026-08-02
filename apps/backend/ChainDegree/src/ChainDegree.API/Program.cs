using ChainDegree.API.Extensions;
using ChainDegree.API.Filters;
using ChainDegree.Core.Application;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Prometheus;
using Scalar.AspNetCore;

using ChainDegree.Reputation;

using System.Threading.Tasks;

namespace ChainDegree.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (builder.Environment.IsDevelopment())
            {
                DotNetEnv.Env.TraversePath().Load();
            }
            builder.Configuration.AddEnvironmentVariables();

            // Register application services
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);

            // Module Toggle: Reputation Engine
            var reputationEnabled = builder.Configuration.GetValue<bool>("Modules:Reputation:Enabled", true);
            if (reputationEnabled)
            {
                builder.Services.AddReputationModule(builder.Configuration);
            }

            // Register CORS for frontend integration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Register config options
            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection(JwtOptions.SectionName));

            // Register health checks (liveness vs readiness)
            builder.Services.AddHealthChecks()
                .AddCheck("Self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"), tags: new[] { "live" })
                .AddDbContextCheck<ChainDegreeDbContext>("Database", tags: new[] { "ready" })
                .AddCheck<ChainDegree.Core.Infrastructure.Monitoring.BesuRpcHealthCheck>("BesuRPC", tags: new[] { "ready" });

            // Register custom filters and factories
            builder.Services.AddScoped<GlobalExceptionFilterAttribute>();
            builder.Services.AddScoped<ChainDegree.API.Filters.IdempotencyFilterAttribute>();
            builder.Services.AddSingleton<ProblemDetailsFactory, ChainDegreeProblemDetailsFactory>();

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<GlobalExceptionFilterAttribute>();
            });

            // Register authorization policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Roles.Registrar, policy => policy.RequireRole(Roles.Registrar));
                options.AddPolicy(Roles.Student, policy => policy.RequireRole(Roles.Student));
                options.AddPolicy(Roles.Recruiter, policy => policy.RequireRole(Roles.Recruiter));
                options.AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin));
                options.AddPolicy(Roles.System, policy => policy.RequireRole(Roles.System));
            });

            // Register rate limiting for public verify endpoint
            builder.Services.AddCustomRateLimiting();

            builder.Services.AddOpenApi();

            var app = builder.Build();

            app.UseMiddleware<ChainDegree.API.Middleware.CorrelationIdMiddleware>();

            app.UseCors("AllowAll");
            app.UseRateLimiter();

            var authProvider = builder.Configuration.GetValue<string>("Modules:Authentication:Provider") ?? "Mock";
            if (string.Equals(authProvider, "Mock", StringComparison.OrdinalIgnoreCase))
            {
                // Mock/Fake Auth Middleware for local development and testing
                app.Use(async (context, next) =>
                {
                    var role = context.Request.Headers.TryGetValue("X-Role", out var r) ? r.ToString() : "Registrar";
                    var userId = context.Request.Headers.TryGetValue("X-User-Id", out var u) ? u.ToString() : "00000000-0000-0000-0000-000000000001";
                    var instId = context.Request.Headers.TryGetValue("X-Institution-Id", out var i) ? i.ToString() : "11111111-1111-1111-1111-111111111111";

                    var identity = new System.Security.Claims.ClaimsIdentity(new[]
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role),
                        new System.Security.Claims.Claim("InstitutionId", instId)
                    }, "FakeAuth");

                    context.User = new System.Security.Claims.ClaimsPrincipal(identity);
                    await next();
                });
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                app.MapScalarApiReference(options =>
                {
                    options.Title = "ChainDegree API";
                });
            }

            // Seed Data: Exclusively when explicitly enabled in config or triggered via --seed argument
            bool shouldSeed = args.Contains("--seed") || builder.Configuration.GetValue<bool>("SeedData:Enabled", false);
            if (shouldSeed)
            {
                try
                {
                    using (var scope = app.Services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
                        await ChainDegreeDbSeeder.SeedAsync(context);
                    }
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "Seed Data execution skipped due to database connection or state issue.");
                }
            }

            app.MapMetrics();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            });
            app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
            app.MapHealthChecks("/health");
            app.MapControllers();

            app.Run();
        }
    }
}
