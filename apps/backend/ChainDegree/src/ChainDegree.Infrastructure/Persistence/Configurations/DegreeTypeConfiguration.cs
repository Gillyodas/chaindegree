using ChainDegree.Core.Domain.Universities.Entities;
using ChainDegree.Core.Domain.Universities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class DegreeTypeConfiguration : BaseEntityConfiguration<DegreeType>
    {
        public override void Configure(EntityTypeBuilder<DegreeType> builder)
        {
            builder.ToTable("DEGREE_TYPES");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.Name)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.HasOne<EducationInstitution>()
                   .WithMany(e => e.DegreeTypes)
                   .HasForeignKey(x => x.InstitutionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.InstitutionId, x.Code }).IsUnique();

            base.Configure(builder);
        }
    }
}
