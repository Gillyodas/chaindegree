using ChainDegree.Reputation.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Reputation.Infrastructure.Persistence;

public class ReputationDbContext : DbContext
{
    public ReputationDbContext(DbContextOptions<ReputationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReputationScore> ReputationScores => Set<ReputationScore>();
    public DbSet<ReputationHistory> ReputationHistories => Set<ReputationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReputationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
