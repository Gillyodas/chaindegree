using ChainDegree.Core.Domain.Reputation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations;

public class ReputationHistoryConfiguration : BaseEntityConfiguration<ReputationHistory>
{
    public override void Configure(EntityTypeBuilder<ReputationHistory> builder)
    {
        builder.ToTable("REPUTATION_HISTORIES");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReputationScoreId).IsRequired();
        builder.Property(x => x.UniversityId).IsRequired();

        builder.Property(x => x.EventId).IsRequired();
        builder.HasIndex(x => x.EventId).IsUnique(); // Idempotency Unique Constraint

        builder.Property(x => x.ScoreChange).IsRequired();
        builder.Property(x => x.NewScore).IsRequired();

        builder.Property(x => x.ReasonCode)
               .HasConversion<string>()
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.Property(x => x.AnchorStatus)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.HistoryHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TxHash).HasMaxLength(66);

        builder.Property(x => x.Timestamp).IsRequired();

        base.Configure(builder);
    }
}
