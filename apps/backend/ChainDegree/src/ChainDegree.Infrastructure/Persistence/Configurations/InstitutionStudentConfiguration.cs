using ChainDegree.Core.Domain.Universities.Entities;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChainDegree.Core.Infrastructure.Persistence.Configurations
{
    public class InstitutionStudentConfiguration : BaseEntityConfiguration<InstitutionStudent>
    {
        public override void Configure(EntityTypeBuilder<InstitutionStudent> builder)
        {
            builder.ToTable("INSTITUTION_STUDENTS");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StudentCode)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.EnrolledAt)
                   .IsRequired();

            builder.HasOne<EducationInstitution>()
                   .WithMany()
                   .HasForeignKey(x => x.InstitutionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Student>()
                   .WithMany()
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.InstitutionId, x.StudentId }).IsUnique();
            builder.HasIndex(x => new { x.InstitutionId, x.StudentCode }).IsUnique();

            base.Configure(builder);
        }
    }
}
