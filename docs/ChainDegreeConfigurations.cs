// ============================================================================
// EF Core Fluent API Configurations cho ChainDegree
// Đặt trong tầng Infrastructure (vd: Infrastructure/Persistence/Configurations/)
// Mỗi class nên tách thành 1 file riêng đặt tên theo entity (chuẩn convention),
// ở đây để gộp 1 file cho dễ review.
//
// LƯU Ý: AUTH_USERS không được cấu hình ở đây vì thuộc module Auth độc lập,
// chỉ liên kết logic qua user_id (không tạo FK cứng) — đúng như ERD chú thích
// "Module Auth quản lý độc lập". DbContext của ChainDegree không nên có
// DbSet<AuthUser> nếu Auth là bounded context khác / service khác.
// ============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChainDegree.Infrastructure.Persistence.Configurations;

// ----------------------------------------------------------------------------
// EDUCATION_INSTITUTIONS
// ----------------------------------------------------------------------------
public class EducationInstitutionConfiguration : IEntityTypeConfiguration<EducationInstitution>
{
    public void Configure(EntityTypeBuilder<EducationInstitution> builder)
    {
        builder.ToTable("EDUCATION_INSTITUTIONS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();

        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}

// ----------------------------------------------------------------------------
// REGISTRARS — thuộc Institution, liên kết logic sang Auth (không FK cứng)
// ----------------------------------------------------------------------------
public class RegistrarConfiguration : IEntityTypeConfiguration<Registrar>
{
    public void Configure(EntityTypeBuilder<Registrar> builder)
    {
        builder.ToTable("REGISTRARS");
        builder.HasKey(x => x.Id);

        // user_id: liên kết logic sang AUTH_USERS, KHÔNG cấu hình HasOne/FK
        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.EmployeeCode).IsUnique();

        builder.Property(x => x.FullName).HasMaxLength(255).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Registrar thuộc 1 Institution — FK thật, vì cùng bounded context Core Domain
        builder.HasOne<EducationInstitution>()
               .WithMany()
               .HasForeignKey(x => x.InstitutionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

// ----------------------------------------------------------------------------
// COMPANIES
// ----------------------------------------------------------------------------
public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("COMPANIES");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName).HasMaxLength(255).IsRequired();

        builder.Property(x => x.BusinessLicenseCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BusinessLicenseCode).IsUnique();

        builder.Property(x => x.ContactEmail).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.ContactEmail).IsUnique();

        builder.Property(x => x.IsVerified).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // KHÔNG cấu hình WithMany(c => c.Jobs): Company và Job là 2 aggregate root
        // độc lập theo DDD — Company không nên giữ navigation collection trỏ
        // sang aggregate khác. Muốn lấy Jobs theo Company thì query qua
        // IJobRepository.GetByCompanyIdAsync(...), không load qua navigation.
    }
}

// ----------------------------------------------------------------------------
// RECRUITER_AGENTS — thuộc Company, liên kết logic sang Auth
// ----------------------------------------------------------------------------
public class RecruiterAgentConfiguration : IEntityTypeConfiguration<RecruiterAgent>
{
    public void Configure(EntityTypeBuilder<RecruiterAgent> builder)
    {
        builder.ToTable("RECRUITER_AGENTS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.FullName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ProfessionalTitle).HasMaxLength(100);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne<Company>()
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

// ----------------------------------------------------------------------------
// STUDENTS
// ----------------------------------------------------------------------------
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("STUDENTS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.StudentCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.StudentCode).IsUnique();

        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.FullName).HasMaxLength(255).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}

// ----------------------------------------------------------------------------
// DEGREES — entity trung tâm, nhiều FK + dữ liệu liên quan blockchain
// ----------------------------------------------------------------------------
public class DegreeConfiguration : IEntityTypeConfiguration<Degree>
{
    public void Configure(EntityTypeBuilder<Degree> builder)
    {
        builder.ToTable("DEGREES");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DegreeCode).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.DegreeCode).IsUnique();

        builder.Property(x => x.Major).HasMaxLength(255);
        builder.Property(x => x.Classification).HasMaxLength(50);

        // Snapshot dữ liệu thô phục vụ băm đối chiếu — có thể lớn, dùng nvarchar(max)
        builder.Property(x => x.PlainDataJson).HasColumnType("nvarchar(max)");

        // salt + hash: độ dài cố định, không cần nvarchar(max) — tránh lãng phí
        builder.Property(x => x.Salt).HasMaxLength(64);
        builder.Property(x => x.DataHashLocal).HasMaxLength(128); // SHA-256 hex = 64, dự phòng SHA-512

        builder.Property(x => x.StatusEnum)
               .HasConversion<string>()
               .HasMaxLength(50)
               .IsRequired();

        // Ethereum/Besu tx hash: "0x" + 64 hex char = 66 ký tự
        builder.Property(x => x.TxHashBlockchain).HasMaxLength(66);

        builder.Property(x => x.IssuedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne<EducationInstitution>()
               .WithMany()
               .HasForeignKey(x => x.InstitutionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Registrar>()
               .WithMany()
               .HasForeignKey(x => x.RegistrarId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Student>()
               .WithMany()
               .HasForeignKey(x => x.StudentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

// ----------------------------------------------------------------------------
// REPORTS — báo cáo về 1 Degree, reporter có thể là Student hoặc RecruiterAgent
// (reporter_id không enforce FK cứng vì trỏ tới 1 trong 2 loại bảng khác nhau)
// ----------------------------------------------------------------------------
public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("REPORTS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReporterRoleEnum).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReporterId).IsRequired();
        builder.HasIndex(x => x.ReporterId);

        builder.Property(x => x.ReportTypeEnum).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.EvidenceUrl).HasMaxLength(500);

        builder.Property(x => x.StatusEnum).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ReviewedAt);

        builder.HasOne<Degree>()
               .WithMany()
               .HasForeignKey(x => x.DegreeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

// ----------------------------------------------------------------------------
// JOBS
// ----------------------------------------------------------------------------
public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("JOBS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.SalaryMin);
        builder.Property(x => x.SalaryMax);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.StatusEnum).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Company -> Job: chỉ FK, KHÔNG navigation 2 chiều (xem ghi chú ở CompanyConfiguration)
        builder.HasOne<Company>()
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Cascade); // xoá Company thì xoá Job liên quan

        builder.HasOne<RecruiterAgent>()
               .WithMany()
               .HasForeignKey(x => x.CreatorAgentId)
               .OnDelete(DeleteBehavior.Restrict);

        // JobDegreeFilter là entity con trong Aggregate Job (owned lifecycle) ->
        // cấu hình quan hệ tại đây, Cascade hợp lý vì filter không có nghĩa
        // tồn tại độc lập ngoài Job. Nếu Domain expose qua backing field
        // (private List<JobDegreeFilter> _filters), cần thêm:
        // builder.Navigation(x => x.JobDegreeFilters)
        //        .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.JobDegreeFilters)
               .WithOne()
               .HasForeignKey(x => x.JobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// ----------------------------------------------------------------------------
// JOB_DEGREE_FILTERS — chỉ cấu hình scalar property, quan hệ đã khai báo
// ở JobConfiguration (tránh duplicate/conflict configuration)
// ----------------------------------------------------------------------------
public class JobDegreeFilterConfiguration : IEntityTypeConfiguration<JobDegreeFilter>
{
    public void Configure(EntityTypeBuilder<JobDegreeFilter> builder)
    {
        builder.ToTable("JOB_DEGREE_FILTERS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DegreeTypeEnum).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.RequiredMajor).HasMaxLength(255);
        builder.Property(x => x.MinClassification).HasMaxLength(50);
    }
}

// ----------------------------------------------------------------------------
// APPLICATIONS — bảng N-N có payload giữa Job, Student, Degree
// ----------------------------------------------------------------------------
public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("APPLICATIONS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RankStatusEnum).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProcessStatusEnum).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne<Job>()
               .WithMany()
               .HasForeignKey(x => x.JobId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Student>()
               .WithMany()
               .HasForeignKey(x => x.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Degree>()
               .WithMany()
               .HasForeignKey(x => x.DegreeId)
               .OnDelete(DeleteBehavior.Restrict);

        // Tránh apply nhiều cascade path tới cùng 1 bảng (SQL Server không cho phép
        // multiple cascade paths) — đây là lý do cả 3 FK trên đều Restrict, không
        // Cascade, dù về nghiệp vụ có thể muốn xoá Application khi xoá Job/Student.
        // Nếu cần xoá theo, xử lý ở Application layer (soft delete / explicit query),
        // không nên ép DB cascade.
    }
}

// ----------------------------------------------------------------------------
// BEHAVIOR_LOGS — log thuần, KHÔNG enforce FK vì actor_id/target_id trỏ tới
// nhiều loại bảng khác nhau tuỳ action_type (polymorphic reference)
// ----------------------------------------------------------------------------
public class BehaviorLogConfiguration : IEntityTypeConfiguration<BehaviorLog>
{
    public void Configure(EntityTypeBuilder<BehaviorLog> builder)
    {
        builder.ToTable("BEHAVIOR_LOGS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionTypeEnum).HasConversion<string>().HasMaxLength(50).IsRequired();

        // Lưu chuỗi thuần, không enum, để độc lập với module Auth
        builder.Property(x => x.ActorRole).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ActorId).IsRequired();
        builder.HasIndex(x => x.ActorId);

        builder.Property(x => x.TargetTable).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetId).IsRequired();
        builder.HasIndex(x => new { x.TargetTable, x.TargetId });

        builder.Property(x => x.OldValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IpAddress).HasMaxLength(45); // đủ cho IPv6

        builder.Property(x => x.CreatedAt).IsRequired();
    }
}

// ----------------------------------------------------------------------------
// REPUTATION_HISTORIES
// ----------------------------------------------------------------------------
public class ReputationHistoryConfiguration : IEntityTypeConfiguration<ReputationHistory>
{
    public void Configure(EntityTypeBuilder<ReputationHistory> builder)
    {
        builder.ToTable("REPUTATION_HISTORIES");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OldScore).IsRequired();
        builder.Property(x => x.NewScore).IsRequired();
        builder.Property(x => x.ChangeReason).HasMaxLength(500);

        builder.Property(x => x.StatusEnum).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.TxHashBlockchain).HasMaxLength(66);

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<EducationInstitution>()
               .WithMany()
               .HasForeignKey(x => x.InstitutionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
