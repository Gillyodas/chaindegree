using ChainDegree.Reputation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChainDegree.Reputation.Infrastructure.Persistence.Configurations;

public class ReputationScoreConfiguration : BaseEntityConfiguration<ReputationScore>
{
    public override void Configure(EntityTypeBuilder<ReputationScore> builder)
    {
        builder.ToTable("REPUTATION_SCORES");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UniversityId).IsRequired();
        builder.HasIndex(x => x.UniversityId).IsUnique();

        builder.Property(x => x.CurrentScore).IsRequired().HasDefaultValue(1000);
        builder.Property(x => x.IsFrozen).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.Histories)
               .WithOne()
               .HasForeignKey(x => x.ReputationScoreId)
               .OnDelete(DeleteBehavior.Cascade);

        base.Configure(builder);
    }
}
