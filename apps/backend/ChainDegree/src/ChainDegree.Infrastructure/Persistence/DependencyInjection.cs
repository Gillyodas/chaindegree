using System;
using System.Collections.Generic;
using System.Text;
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
            // 1. Đăng ký interceptor TRƯỚC, vì AddDbContext cần resolve nó qua sp
            services.AddScoped<DispatchDomainEventsInterceptor>();

            // 2. Đăng ký DbContext, lấy interceptor từ container
            services.AddDbContext<ChainDegreeDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("ChainDegree");
                options.UseSqlServer(connectionString)
                       .AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>());
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // ... các repo khác

            return services;
        }
    }
}
