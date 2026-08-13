using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using ChainDegree.Core.Domain.Universities;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class BatchRecordConfiguration : IEntityTypeConfiguration<BatchRecord>
    {
        public void Configure(EntityTypeBuilder<BatchRecord> builder)
        {
            builder.ToTable("BATCH_RECORDS");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.BatchName).HasMaxLength(150).IsRequired();
            builder.HasIndex(x => x.BatchName).IsUnique();

            builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
            builder.Property(x => x.DegreeCount).IsRequired();

            builder.Property(x => x.MerkleRoot).HasMaxLength(128);
            builder.HasIndex(x => x.MerkleRoot).IsUnique().HasFilter("[MerkleRoot] IS NOT NULL");
            builder.Property(x => x.TxHash).HasMaxLength(66);
            builder.Property(x => x.BlockNumber);
            builder.Property(x => x.EstimatedWaitTimeSeconds).IsRequired();
            builder.Property(x => x.FailureReason).HasColumnType("nvarchar(max)");

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.CompletedAt);

            builder.HasOne<EducationInstitution>()
                   .WithMany()
                   .HasForeignKey(x => x.InstitutionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
