using ChainDegree.API.Filters;
using ChainDegree.Core.Application;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Scalar.AspNetCore;

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

            builder.Services.Configure<BesuOptions>(
                builder.Configuration.GetSection("Blockchain:Besu"));

            // Register health checks (EF Core database check)
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<ChainDegreeDbContext>("Database");

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

            builder.Services.AddOpenApi();

            var app = builder.Build();

            app.UseMiddleware<ChainDegree.API.Middleware.CorrelationIdMiddleware>();

            app.UseCors("AllowAll");

            if (app.Environment.IsDevelopment())
            {
                // Fake Auth Middleware to auto-populate ClaimsPrincipal based on dev headers or fallback defaults
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

                app.MapOpenApi();

                app.MapScalarApiReference(options =>
                {
                    options.Title = "ChainDegree API";
                });

                // Auto Seed Development Database
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
                    await ChainDegreeDbSeeder.SeedAsync(context);
                }
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllers();

            app.Run();
        }
    }
}
