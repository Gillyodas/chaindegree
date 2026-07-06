using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class DegreeProcessingRecordConfiguration : IEntityTypeConfiguration<DegreeProcessingRecord>
    {
        public void Configure(EntityTypeBuilder<DegreeProcessingRecord> builder)
        {
            builder.ToTable("DEGREE_PROCESSING_RECORDS");

            builder.HasKey(x => x.DegreeId);
            
            builder.Property(x => x.DegreeId).ValueGeneratedNever();

            builder.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);
            builder.Property(x => x.NextRetryAt);
            builder.Property(x => x.LastRetryAt);
            builder.Property(x => x.LeaseUntil);
            builder.Property(x => x.WorkerId).HasMaxLength(100);

            // Setup relationship to Degree
            builder.HasOne<Degree>()
                   .WithOne()
                   .HasForeignKey<DegreeProcessingRecord>(x => x.DegreeId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
