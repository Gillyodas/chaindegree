using ChainDegree.Core.Domain.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OUTBOX_MESSAGES");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventType)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(x => x.Payload)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired();

            builder.Property(x => x.OccurredOn)
                   .IsRequired();

            builder.Property(x => x.ProcessedOn);

            builder.Property(x => x.Error)
                   .HasColumnType("nvarchar(max)");

            builder.Property(x => x.RetryCount)
                   .IsRequired();

            builder.HasIndex(x => x.ProcessedOn);
        }
    }
}
