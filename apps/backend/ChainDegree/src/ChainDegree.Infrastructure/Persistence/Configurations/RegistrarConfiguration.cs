using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Domain.Universities.Entities;
using ChainDegree.Core.Domain.Auth;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class RegistrarConfiguration : BaseEntityConfiguration<Registrar>
    {
        public override void Configure(EntityTypeBuilder<Registrar> builder)
        {
            builder.ToTable("REGISTRARS");
            builder.HasKey(x => x.Id);

            builder.HasOne<AuthUser>()
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => x.UserId);

            builder.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
            builder.HasIndex(x => x.EmployeeCode).IsUnique();

            builder.Property(x => x.FullName).HasMaxLength(255).IsRequired();

            // Registrar belongs to 1 Institution
            builder.HasOne<EducationInstitution>()
                   .WithMany(e => e.Registrars)
                   .HasForeignKey(x => x.InstitutionId)
                   .OnDelete(DeleteBehavior.Restrict);

            base.Configure(builder);
        }
    }
}
