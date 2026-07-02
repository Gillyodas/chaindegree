using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Infrastructure.Persistence.Interceptors;
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
            // Register interceptors
            services.AddScoped<AuditableEntityInterceptor>();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<DispatchDomainEventsInterceptor>();

            // Register DbContext
            services.AddDbContext<ChainDegreeDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("ChainDegree");
                options.UseSqlServer(connectionString)
                       .AddInterceptors(
                           sp.GetRequiredService<AuditableEntityInterceptor>(),
                           sp.GetRequiredService<SoftDeleteInterceptor>(),
                           sp.GetRequiredService<DispatchDomainEventsInterceptor>());
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
