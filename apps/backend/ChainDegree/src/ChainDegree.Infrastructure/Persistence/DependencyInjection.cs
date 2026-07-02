using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Infrastructure.Persistence.Interceptors;
using ChainDegree.Core.Infrastructure.Persistence.Outbox;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Auth;
using ChainDegree.Core.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {
            // Register HTTP context and auth abstractions
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserAccessor, FakeCurrentUserAccessor>();
            services.AddScoped<IInstitutionOwnershipChecker, FakeInstitutionOwnershipChecker>();
            services.AddScoped<IRoleChecker, FakeRoleChecker>();

            // Register services
            services.AddScoped<IBehaviorLogService, BehaviorLogService>();

            // Register configurations
            services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

            // Register background workers
            services.AddHostedService<OutboxProcessor>();

            // Register interceptors
            services.AddScoped<AuditableEntityInterceptor>();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<ConvertDomainEventsToOutboxInterceptor>();

            // Register DbContext
            services.AddDbContext<ChainDegreeDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("ChainDegree");
                options.UseSqlServer(connectionString)
                       .AddInterceptors(
                           sp.GetRequiredService<AuditableEntityInterceptor>(),
                           sp.GetRequiredService<SoftDeleteInterceptor>(),
                           sp.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>());
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
