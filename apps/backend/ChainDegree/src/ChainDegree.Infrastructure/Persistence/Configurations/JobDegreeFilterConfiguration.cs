using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Jobs.Entities;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class JobDegreeFilterConfiguration : BaseEntityConfiguration<JobDegreeFilter>
    {
        public override void Configure(EntityTypeBuilder<JobDegreeFilter> builder)
        {
            builder.ToTable("JOB_DEGREE_FILTERS");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DegreeType)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.RequiredMajor).HasMaxLength(255);
            builder.Property(x => x.MinClassification).HasMaxLength(50);

            builder.HasOne<Job>()
                   .WithMany(j => j.JobDegreeFilters)
                   .HasForeignKey(x => x.JobId)
                   .OnDelete(DeleteBehavior.Cascade);

            base.Configure(builder);
        }
    }
}
