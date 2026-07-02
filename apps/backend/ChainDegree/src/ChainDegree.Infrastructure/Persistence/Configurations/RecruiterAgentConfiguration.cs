using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChainDegree.Core.Domain.Recruiters;
using ChainDegree.Core.Domain.Recruiters.Entities;
using ChainDegree.Core.Domain.Auth;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class RecruiterAgentConfiguration : BaseEntityConfiguration<RecruiterAgent>
    {
        public override void Configure(EntityTypeBuilder<RecruiterAgent> builder)
        {
            builder.ToTable("RECRUITER_AGENTS");
            builder.HasKey(x => x.Id);

            builder.HasOne<AuthUser>()
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => x.UserId);

            builder.Property(x => x.AgentName).HasMaxLength(255).IsRequired();

            builder.HasOne<Company>()
                   .WithMany(c => c.RecruiterAgents)
                   .HasForeignKey(x => x.CompanyId)
                   .OnDelete(DeleteBehavior.Restrict);

            base.Configure(builder);
        }
    }
}
