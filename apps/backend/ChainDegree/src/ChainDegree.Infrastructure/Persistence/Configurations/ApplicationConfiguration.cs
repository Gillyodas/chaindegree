using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Students;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class ApplicationConfiguration : BaseEntityConfiguration<ChainDegree.Core.Domain.Applications.Application>
    {
        public override void Configure(EntityTypeBuilder<ChainDegree.Core.Domain.Applications.Application> builder)
        {
            builder.ToTable("APPLICATIONS");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RankStatus)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.ProcessStatus)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.IsForceSubmitted).IsRequired();

            builder.HasOne<Job>()
                   .WithMany()
                   .HasForeignKey(x => x.JobId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Student>()
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.AttachedDegrees)
                   .WithOne()
                   .HasForeignKey(x => x.ApplicationId)
                   .OnDelete(DeleteBehavior.Cascade);

            base.Configure(builder);
        }
    }
}
