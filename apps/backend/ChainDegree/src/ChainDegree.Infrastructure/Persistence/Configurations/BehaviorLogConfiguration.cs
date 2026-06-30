using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.SharedKernel;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations;

public class BehaviorLogConfiguration : IEntityTypeConfiguration<BehaviorLog>
{
    public void Configure(EntityTypeBuilder<BehaviorLog> builder)
    {
        builder.ToTable("BEHAVIOR_LOGS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionType)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        // Lưu chuỗi thuần, không enum, để độc lập với module Auth
        builder.Property(x => x.ActorRole).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ActorId).IsRequired();
        builder.HasIndex(x => x.ActorId);

        builder.Property(x => x.TargetTable).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetId).IsRequired();
        builder.HasIndex(x => new { x.TargetTable, x.TargetId });

        builder.Property(x => x.OldValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IpAddress).HasMaxLength(45); // đủ cho IPv6

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
