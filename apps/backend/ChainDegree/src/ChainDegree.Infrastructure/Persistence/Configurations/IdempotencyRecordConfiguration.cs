using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Infrastructure.Persistence.Entities;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IDEMPOTENCY_RECORDS");

            builder.HasKey(x => x.IdempotencyKey);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();

            builder.Property(x => x.ResponseBodyJson).HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(x => x.ResponseStatusCode).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();

            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}
