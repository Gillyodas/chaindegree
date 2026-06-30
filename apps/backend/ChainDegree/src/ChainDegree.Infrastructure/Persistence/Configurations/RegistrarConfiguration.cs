using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Domain.Universities.Entities;
using ChainDegree.Core.Domain.Auth;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations;

public class RegistrarConfiguration : IEntityTypeConfiguration<Registrar>
{
    public void Configure(EntityTypeBuilder<Registrar> builder)
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

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Registrar thuộc 1 Institution — FK thật, vì cùng bounded context Core Domain
        builder.HasOne<EducationInstitution>()
               .WithMany(e => e.Registrars)
               .HasForeignKey(x => x.InstitutionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
