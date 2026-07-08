using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class DegreeUpdateRequestConfiguration : BaseEntityConfiguration<DegreeUpdateRequest>
    {
        public override void Configure(EntityTypeBuilder<DegreeUpdateRequest> builder)
        {
            builder.ToTable("DEGREE_UPDATE_REQUESTS");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.DegreeId).IsRequired();
            builder.Property(x => x.Major).IsRequired().HasMaxLength(250);
            builder.Property(x => x.Classification).IsRequired().HasMaxLength(100);

            // CryptoSnapshot Value Object Configuration
            builder.OwnsOne(x => x.CryptoData, cb =>
            {
                cb.Property(c => c.PlainDataJson).HasColumnName("PlainDataJson").HasColumnType("nvarchar(max)").IsRequired();
                cb.Property(c => c.Salt).HasColumnName("Salt").HasMaxLength(64).IsRequired();
                cb.Property(c => c.DataHashLocal).HasColumnName("DataHashLocal").HasMaxLength(128).IsRequired();
            });

            // DegreeActionReason Value Object Configuration
            builder.OwnsOne(x => x.Reason, rb =>
            {
                rb.Property(r => r.Code).HasColumnName("ReasonCode").HasMaxLength(50).IsRequired();
                rb.Property(r => r.Description).HasColumnName("ReasonDescription").HasMaxLength(1000).IsRequired();
            });

            // Setup relationship to Degree
            builder.HasOne<Degree>()
                   .WithOne()
                   .HasForeignKey<DegreeUpdateRequest>(x => x.DegreeId)
                   .OnDelete(DeleteBehavior.Cascade);

            base.Configure(builder);
        }
    }
}
