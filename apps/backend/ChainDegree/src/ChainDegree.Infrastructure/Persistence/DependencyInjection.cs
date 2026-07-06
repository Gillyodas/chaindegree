using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Infrastructure.Persistence.Interceptors;
using ChainDegree.Core.Infrastructure.Persistence.Outbox;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Auth;
using ChainDegree.Core.Infrastructure.Services;
using ChainDegree.Core.Infrastructure.Persistence.Locking;
using ChainDegree.Core.Infrastructure.Persistence.Repositories;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Infrastructure.Cryptography.Services;
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
            services.AddSingleton<IJsonCanonicalizer, JsonCanonicalizer>();
            services.AddSingleton<IHashService, Sha256HashService>();
            services.AddScoped<IPendingDegreeLockStrategy, SqlServerPendingDegreeLockStrategy>();
            services.AddScoped<IDegreeRepository, DegreeRepository>();
            services.AddScoped<Core.Application.Abstractions.Queries.IBatchQueryService, BatchTrackingService>();

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
