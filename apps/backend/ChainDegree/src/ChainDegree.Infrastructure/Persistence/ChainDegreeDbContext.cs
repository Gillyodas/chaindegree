using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
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
using DomainApplication = ChainDegree.Core.Domain.Applications.Application;

namespace ChainDegree.Core.Infrastructure.Persistence
{
    public class ChainDegreeDbContext : DbContext
    {
        public ChainDegreeDbContext(DbContextOptions<ChainDegreeDbContext> options)
            : base(options)
        {
        }

        public DbSet<AuthUser> AuthUsers => Set<AuthUser>();
        public DbSet<EducationInstitution> EducationInstitutions => Set<EducationInstitution>();
        public DbSet<Registrar> Registrars => Set<Registrar>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<RecruiterAgent> RecruiterAgents => Set<RecruiterAgent>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Degree> Degrees => Set<Degree>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<Job> Jobs => Set<Job>();
        public DbSet<JobDegreeFilter> JobDegreeFilters => Set<JobDegreeFilter>();
        public DbSet<DomainApplication> Applications => Set<DomainApplication>();
        public DbSet<ApplicationAttachedDegree> ApplicationAttachedDegrees => Set<ApplicationAttachedDegree>();
        public DbSet<BehaviorLog> BehaviorLogs => Set<BehaviorLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChainDegreeDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
