using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Recruiters;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class CompanyConfiguration : BaseEntityConfiguration<Company>
    {
        public override void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("COMPANIES");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();

            builder.Property(x => x.BusinessLicenseCode).HasMaxLength(50).IsRequired();
            builder.HasIndex(x => x.BusinessLicenseCode).IsUnique();

            builder.Property(x => x.ContactEmail).HasMaxLength(255).IsRequired();
            builder.HasIndex(x => x.ContactEmail).IsUnique();

            builder.Property(x => x.IsVerified).IsRequired();

            builder.Property(x => x.CompanyStatus)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            base.Configure(builder);
        }
    }
}
