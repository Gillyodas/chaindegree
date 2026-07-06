using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Domain.Universities.Entities;
using ChainDegree.Core.Domain.Students;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class DegreeConfiguration : BaseEntityConfiguration<Degree>
    {
        public override void Configure(EntityTypeBuilder<Degree> builder)
        {
            builder.ToTable("DEGREES");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DegreeCode).HasMaxLength(100).IsRequired();
            builder.HasIndex(x => x.DegreeCode).IsUnique();

            builder.Property(x => x.Major).HasMaxLength(255).IsRequired();
            builder.Property(x => x.Classification).HasMaxLength(50).IsRequired();

            // Map CryptoData as an owned value object (CryptoSnapshot)
            builder.OwnsOne(x => x.CryptoData, cb =>
            {
                cb.Property(c => c.PlainDataJson)
                  .HasColumnName("PlainDataJson")
                  .HasColumnType("nvarchar(max)")
                  .IsRequired();

                cb.Property(c => c.Salt)
                  .HasColumnName("Salt")
                  .HasMaxLength(64)
                  .IsRequired();

                cb.Property(c => c.DataHashLocal)
                  .HasColumnName("DataHashLocal")
                  .HasMaxLength(128)
                  .IsRequired();
            });

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            // Ethereum/Besu tx hash: "0x" + 64 hex char = 66 chars
            builder.Property(x => x.TxHashBlockchain).HasMaxLength(66);

            builder.Property(x => x.IssuedAt).IsRequired();

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasOne<EducationInstitution>()
                   .WithMany()
                   .HasForeignKey(x => x.InstitutionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Registrar>()
                   .WithMany()
                   .HasForeignKey(x => x.SignedByRegistrarId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Student>()
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            base.Configure(builder);
        }
    }
}
