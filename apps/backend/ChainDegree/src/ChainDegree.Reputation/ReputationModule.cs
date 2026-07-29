using ChainDegree.Reputation.Application.Abstractions;
using ChainDegree.Reputation.Infrastructure.Blockchain;
using ChainDegree.Reputation.Infrastructure.Persistence;
using ChainDegree.Reputation.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChainDegree.Reputation;

public static class ReputationModule
{
    public static IServiceCollection AddReputationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. DbContext
        var connectionString = configuration.GetConnectionString("ChainDegree");
        services.AddDbContext<ReputationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // 2. Repositories & Services
        services.AddScoped<IReputationRepository, ReputationRepository>();
        services.AddScoped<IReputationBlockchainService, NethereumReputationBlockchainService>();

        // 3. MediatR Handlers for Reputation Module
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ReputationModule).Assembly);
        });

        return services;
    }
}
