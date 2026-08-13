using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class DegreeVersionConfiguration : BaseEntityConfiguration<DegreeVersion>
    {
        public override void Configure(EntityTypeBuilder<DegreeVersion> builder)
        {
            builder.ToTable("DEGREE_VERSIONS");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.HasIndex(x => new { x.DegreeId, x.Version }).IsUnique();

            builder.Property(x => x.DegreeId).IsRequired();
            builder.Property(x => x.Version).IsRequired();
            builder.Property(x => x.PreviousHash).IsRequired().HasMaxLength(150);
            builder.Property(x => x.CurrentHash).IsRequired().HasMaxLength(150);
            builder.Property(x => x.BlockchainTxHash).IsRequired().HasMaxLength(150);
            builder.Property(x => x.MerkleProofJson).HasMaxLength(4000);
            builder.Property(x => x.PlainDataJson).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(x => x.Salt).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Major).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Classification).IsRequired().HasMaxLength(100);
            builder.Property(x => x.EffectiveAt).IsRequired();

            // Setup relationship to Degree
            builder.HasOne<Degree>()
                   .WithMany()
                   .HasForeignKey(x => x.DegreeId)
                   .OnDelete(DeleteBehavior.Cascade);

            base.Configure(builder);
        }
    }
}
