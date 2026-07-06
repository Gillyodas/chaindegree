using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using ChainDegree.Core.Application.Common.Behaviors;

namespace ChainDegree.Core.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register all validators from the application assembly
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            // Configure MediatR with pipeline behaviors
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Register Application services
            services.AddScoped<Abstractions.Services.IDegreeIssuanceService, Services.DegreeIssuanceService>();

            return services;
        }
    }
}
