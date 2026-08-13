using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class BatchDegreeRecordConfiguration : IEntityTypeConfiguration<BatchDegreeRecord>
    {
        public void Configure(EntityTypeBuilder<BatchDegreeRecord> builder)
        {
            builder.ToTable("BATCH_DEGREE_RECORDS");

            builder.HasKey(x => new { x.BatchId, x.DegreeId });
            builder.HasIndex(x => new { x.DegreeId, x.Version }).IsUnique();

            builder.Property(x => x.Version).IsRequired();
            builder.Property(x => x.LeafIndex).IsRequired();
            builder.Property(x => x.ProofHashesJson).HasColumnType("nvarchar(max)");

            builder.HasOne<BatchRecord>()
                   .WithMany()
                   .HasForeignKey(x => x.BatchId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Degree>()
                   .WithMany()
                   .HasForeignKey(x => x.DegreeId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
