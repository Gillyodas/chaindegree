using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using ChainDegree.Core.Domain.SharedKernel.Interfaces;
using ChainDegree.Core.Application.Abstractions.Auth;
using DomainApplication = ChainDegree.Core.Domain.Applications.Application;

namespace ChainDegree.Core.Infrastructure.Persistence
{
    public class ChainDegreeDbContext : DbContext
    {
        private readonly ICurrentUserAccessor _currentUserAccessor;
        internal readonly Guid? _currentInstitutionId; // Internal so it can be referenced in reflection/expressions if needed

        public ChainDegreeDbContext(
            DbContextOptions<ChainDegreeDbContext> options,
            ICurrentUserAccessor currentUserAccessor)
            : base(options)
        {
            _currentUserAccessor = currentUserAccessor;
            _currentInstitutionId = _currentUserAccessor.InstitutionId;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChainDegreeDbContext).Assembly);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;

                bool isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);
                bool isInstitutionScoped = typeof(IInstitutionScoped).IsAssignableFrom(clrType);

                if (isSoftDeletable || isInstitutionScoped)
                {
                    var parameter = Expression.Parameter(clrType, "e");
                    Expression? filter = null;

                    if (isSoftDeletable)
                    {
                        var deletedAtProperty = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
                        var nullConstant = Expression.Constant(null, typeof(DateTime?));
                        filter = Expression.Equal(deletedAtProperty, nullConstant);
                    }

                    if (isInstitutionScoped)
                    {
                        var institutionIdProperty = Expression.Property(parameter, nameof(IInstitutionScoped.InstitutionId));
                        var nullableInstitutionId = Expression.Convert(institutionIdProperty, typeof(Guid?));
                        
                        // Reference the DbContext field _currentInstitutionId on 'this' context
                        var contextExpression = Expression.Constant(this);
                        var currentInstIdField = typeof(ChainDegreeDbContext).GetField(nameof(_currentInstitutionId), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var capturedValue = Expression.Field(contextExpression, currentInstIdField!);
                        
                        var equalExpression = Expression.Equal(nullableInstitutionId, capturedValue);

                        if (filter == null)
                        {
                            filter = equalExpression;
                        }
                        else
                        {
                            filter = Expression.AndAlso(filter, equalExpression);
                        }
                    }

                    if (filter != null)
                    {
                        var lambda = Expression.Lambda(filter, parameter);
                        modelBuilder.Entity(clrType).HasQueryFilter(lambda);
                    }
                }
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
