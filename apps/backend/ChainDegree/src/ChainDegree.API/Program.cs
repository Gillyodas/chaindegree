using ChainDegree.Core.Application;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.API.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ChainDegree.API
{
    public class Program
    {
        public static void Main(string[] args)
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

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
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
