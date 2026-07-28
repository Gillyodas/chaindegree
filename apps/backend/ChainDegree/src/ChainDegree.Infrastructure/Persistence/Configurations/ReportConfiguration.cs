using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class ReportConfiguration : BaseEntityConfiguration<Report>
    {
        public override void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.ToTable("REPORTS");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ReporterRole)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(x => x.ReporterId).IsRequired();
            builder.HasIndex(x => x.ReporterId);

            builder.Property(x => x.ReportType)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(2000)
                   .IsRequired();

            builder.Property(x => x.EvidenceFileName)
                   .HasMaxLength(255);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.ReviewedAt);

            builder.Property(x => x.RejectionReason)
                   .HasMaxLength(1000);

            builder.HasIndex(x => new { x.ReporterId, x.TargetDegreeId, x.Status });

            builder.HasOne<Degree>()
                   .WithMany()
                   .HasForeignKey(x => x.TargetDegreeId)
                   .OnDelete(DeleteBehavior.Restrict);

            base.Configure(builder);
        }
    }
}
