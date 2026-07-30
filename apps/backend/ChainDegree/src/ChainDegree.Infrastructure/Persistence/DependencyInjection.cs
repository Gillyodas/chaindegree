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
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Infrastructure.Blockchain;
using ChainDegree.Core.Infrastructure.Cryptography.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Microsoft.Extensions.Options;

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
            services.AddSingleton<IMerkleTreeService, MerkleTreeService>();
            services.AddScoped<IPendingDegreeLockStrategy, SqlServerPendingDegreeLockStrategy>();
            services.AddScoped<IDegreeRepository, DegreeRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IReputationReadService, ReputationReadService>();
            services.AddScoped<IEvidenceStorageService, LocalFileSystemEvidenceStorageService>();
            services.AddScoped<Core.Application.Abstractions.Queries.IBatchQueryService, BatchTrackingService>();
            services.AddScoped<IBlockchainService, NethereumBlockchainService>();
            services.AddSingleton<Monitoring.WorkerMetrics>();

            // Register configurations
            services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
            services.Configure<BlockchainOptions>(configuration.GetSection(BlockchainOptions.SectionName));
            services.AddSingleton<IWeb3>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BlockchainOptions>>().Value;
                var pk = options.PrivateKey;
                if (string.IsNullOrWhiteSpace(pk))
                {
                    throw new System.ArgumentException("Blockchain PrivateKey is not configured.");
                }
                if (pk.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
                {
                    pk = pk.Substring(2);
                }
                var account = new Account(pk);
                return new Web3(account, options.RpcUrl);
            });
            services.AddSingleton<IBlockchainSigner, LocalEnvSigner>();
            services.Configure<BatchingWorkerOptions>(configuration.GetSection(BatchingWorkerOptions.SectionName));

            // Register background workers
            services.AddHostedService<OutboxProcessor>();
            services.AddHostedService<BackgroundWorkers.BatchingDegreeWorker>();
            services.AddHostedService<BlockchainStartupValidatorService>();

            // Register interceptors
            services.AddScoped<AuditableEntityInterceptor>();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<ConvertDomainEventsToOutboxInterceptor>();
            services.AddScoped<DegreeProcessingInterceptor>();

            // Register DbContext
            services.AddDbContext<ChainDegreeDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("ChainDegree");
                options.UseSqlServer(connectionString)
                       .AddInterceptors(
                           sp.GetRequiredService<AuditableEntityInterceptor>(),
                           sp.GetRequiredService<SoftDeleteInterceptor>(),
                           sp.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>(),
                           sp.GetRequiredService<DegreeProcessingInterceptor>());
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
