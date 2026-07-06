using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ChainDegree.Core.Domain.Auth;
using ChainDegree.Core.Domain.Universities;
using ChainDegree.Core.Domain.Universities.Entities;
using ChainDegree.Core.Domain.Recruiters;
using ChainDegree.Core.Domain.Recruiters.Entities;
using ChainDegree.Core.Domain.Students;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Reports;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Jobs.Entities;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Applications.Entities;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Infrastructure.Persistence.QueryFilters;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using DomainApplication = ChainDegree.Core.Domain.Applications.Application;

namespace ChainDegree.Core.Infrastructure.Persistence
{
    public class ChainDegreeDbContext : DbContext
    {
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<ChainDegreeDbContext> _logger;

        // internal (không private) để GlobalQueryFilterApplier — cùng assembly —
        // có thể build Expression.Field trỏ tới field này. Field, không phải
        // property, vì Expression.Field cần trỏ đúng backing storage; giá trị
        // phải được đọc LẠI mỗi lần một DbContext instance khác chạy query,
        // không phải bị "đóng băng" tại thời điểm OnModelCreating.
        internal readonly Guid? _currentInstitutionId;

        public ChainDegreeDbContext(
            DbContextOptions<ChainDegreeDbContext> options,
            ICurrentUserAccessor currentUserAccessor,
            ILogger<ChainDegreeDbContext> logger)
            : base(options)
        {
            _currentUserAccessor = currentUserAccessor;
            _currentInstitutionId = _currentUserAccessor.InstitutionId;
            _logger = logger;
        }

        public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
        public DbSet<EducationInstitution> EducationInstitutions => Set<EducationInstitution>();
        public DbSet<Registrar> Registrars => Set<Registrar>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<RecruiterAgent> RecruiterAgents => Set<RecruiterAgent>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Degree> Degrees => Set<Degree>();
        public DbSet<DegreeType> DegreeTypes => Set<DegreeType>();
        public DbSet<InstitutionStudent> InstitutionStudents => Set<InstitutionStudent>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<JobDegreeFilter> JobDegreeFilters => Set<JobDegreeFilter>();
        public DbSet<DomainApplication> Applications => Set<DomainApplication>();
        public DbSet<ApplicationAttachedDegree> ApplicationAttachedDegrees => Set<ApplicationAttachedDegree>();
        public DbSet<BehaviorLog> BehaviorLogs => Set<BehaviorLog>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
        public DbSet<DegreeProcessingRecord> DegreeProcessingRecords => Set<DegreeProcessingRecord>();
        public DbSet<BatchRecord> BatchRecords => Set<BatchRecord>();
        public DbSet<BatchDegreeRecord> BatchDegreeRecords => Set<BatchDegreeRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Load toàn bộ IEntityTypeConfiguration<T> trong assembly hiện tại
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChainDegreeDbContext).Assembly);

            // 2. Áp global query filter (soft-delete / institution scoping).
            //    Toàn bộ logic reflection + expression-building nằm trong
            //    GlobalQueryFilterApplier — DbContext chỉ gọi, không tự làm.
            var filterApplier = new GlobalQueryFilterApplier(_logger);
            filterApplier.Apply(modelBuilder, dbContextInstance: this);

            base.OnModelCreating(modelBuilder);
        }
    }
}
