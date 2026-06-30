using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Applications.Entities;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations;

public class ApplicationAttachedDegreeConfiguration : IEntityTypeConfiguration<ApplicationAttachedDegree>
{
    public void Configure(EntityTypeBuilder<ApplicationAttachedDegree> builder)
    {
        builder.ToTable("APPLICATION_ATTACHED_DEGREES");
        builder.HasKey(x => new { x.ApplicationId, x.DegreeId });

        builder.HasOne<Degree>()
               .WithMany()
               .HasForeignKey(x => x.DegreeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
