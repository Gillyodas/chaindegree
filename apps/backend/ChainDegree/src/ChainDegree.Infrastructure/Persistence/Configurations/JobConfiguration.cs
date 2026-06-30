using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Recruiters;
using ChainDegree.Core.Domain.Recruiters.Entities;
using ChainDegree.Core.Domain.Universities;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("JOBS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.SalaryMin).HasPrecision(18, 2);
        builder.Property(x => x.SalaryMax).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        
        builder.Property(x => x.Status)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.ApplicationStartDate).IsRequired();
        builder.Property(x => x.ApplicationEndDate).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Company -> Job: chỉ FK, KHÔNG navigation 2 chiều
        builder.HasOne<Company>()
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Cascade); // xoá Company thì xoá Job liên quan

        builder.HasOne<RecruiterAgent>()
               .WithMany()
               .HasForeignKey(x => x.CreatedByAgentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<EducationInstitution>()
               .WithMany()
               .HasForeignKey(x => x.PartnerUniversityId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
