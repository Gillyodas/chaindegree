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
            services.AddSingleton(System.TimeProvider.System);
            services.AddScoped<Abstractions.Services.IDegreeIssuanceService, Services.DegreeIssuanceService>();
            services.AddScoped<Abstractions.Policies.IDegreeDuplicatePolicy, Policies.DegreeDuplicatePolicy>();
            services.AddScoped<Abstractions.Crypto.IDegreeHashService, Services.DegreeHashService>();
            services.AddScoped<Recruitment.Services.IJobRankingService, Recruitment.Services.JobRankingService>();

            return services;
        }
    }
}
