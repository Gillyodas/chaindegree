using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Universities;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class EducationInstitutionConfiguration : BaseEntityConfiguration<EducationInstitution>
    {
        public override void Configure(EntityTypeBuilder<EducationInstitution> builder)
        {
            builder.ToTable("EDUCATION_INSTITUTIONS");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Name).HasMaxLength(255).IsRequired();

            builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
            builder.HasIndex(x => x.Email).IsUnique();

            base.Configure(builder);
        }
    }
}
