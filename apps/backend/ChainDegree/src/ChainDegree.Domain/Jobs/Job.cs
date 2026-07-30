using System;
using System.Collections.Generic;
using System.Linq;
using ChainDegree.Core.Domain.Applications.Enums;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Jobs.Entities;
using ChainDegree.Core.Domain.Jobs.Enums;
using ChainDegree.Core.Domain.SharedKernel;
using ChainDegree.SharedKernel.DomainErrors.Jobs;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Jobs
{
    public class Job : AggregateRoot
    {
        public Guid CompanyId { get; private set; }
        public Guid CreatedByAgentId { get; private set; }
        public Guid? PartnerUniversityId { get; private set; }
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public decimal SalaryMin { get; private set; }
        public decimal SalaryMax { get; private set; }
        public DateTime ApplicationStartDate { get; private set; }
        public DateTime ApplicationEndDate { get; private set; }
        public JobStatusEnum Status { get; private set; }

        public bool IsExpired(DateTimeOffset utcNow) => utcNow.UtcDateTime > ApplicationEndDate || Status == JobStatusEnum.Closed;

        private readonly List<JobDegreeFilter> _jobDegreeFilters = new();
        public IReadOnlyCollection<JobDegreeFilter> JobDegreeFilters => _jobDegreeFilters.AsReadOnly();

        private Job() { }

        private Job(
            Guid id,
            Guid companyId,
            Guid createdByAgentId,
            Guid? partnerUniversityId,
            string title,
            string description,
            decimal salaryMin,
            decimal salaryMax,
            DateTime applicationStartDate,
            DateTime applicationEndDate,
            JobStatusEnum status,
            DateTime utcNow)
        {
            Id = id;
            CompanyId = companyId;
            CreatedByAgentId = createdByAgentId;
            PartnerUniversityId = partnerUniversityId;
            Title = title;
            Description = description;
            SalaryMin = salaryMin;
            SalaryMax = salaryMax;
            ApplicationStartDate = applicationStartDate;
            ApplicationEndDate = applicationEndDate;
            Status = status;
            CreatedAt = utcNow;
        }

        public static Result<Job> Create(
            Guid companyId,
            Guid createdByAgentId,
            Guid? partnerUniversityId,
            string title,
            string description,
            decimal salaryMin,
            decimal salaryMax,
            DateTime? applicationStartDate,
            DateTime applicationEndDate,
            DateTimeOffset utcNow)
        {
            if (companyId == Guid.Empty || createdByAgentId == Guid.Empty)
                return Result<Job>.Failure(JobErrors.EmptyIdentifiers);

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
                return Result<Job>.Failure(JobErrors.MissingJobDetails);

            if (description.Length > 4000)
                return Result<Job>.Failure(JobErrors.DescriptionTooLong);

            // SalaryMin must be > 0 to ensure ln(SalaryAvg) is well-defined
            if (salaryMin <= 0 || salaryMax < salaryMin)
                return Result<Job>.Failure(JobErrors.InvalidSalaryRange);

            DateTime actualStartDate = applicationStartDate ?? utcNow.UtcDateTime;

            if (actualStartDate >= applicationEndDate)
                return Result<Job>.Failure(JobErrors.InvalidDateRange);

            if (applicationEndDate <= utcNow.UtcDateTime)
                return Result<Job>.Failure(JobErrors.EndDateInPast);

            var job = new Job(
                Guid.NewGuid(),
                companyId,
                createdByAgentId,
                partnerUniversityId,
                title,
                description,
                salaryMin,
                salaryMax,
                actualStartDate,
                applicationEndDate,
                JobStatusEnum.Active,
                utcNow.UtcDateTime
            );

            return Result<Job>.Success(job);
        }

        public void AddFilter(DegreeTypeEnum degreeType, string requiredMajor, string minClassification)
        {
            var filter = new JobDegreeFilter(Id, degreeType, requiredMajor, minClassification);
            _jobDegreeFilters.Add(filter);
            UpdatedAt = DateTime.UtcNow;
        }

        public ApplicationRankStatusEnum EvaluateApplication(Degree studentDegree)
        {
            if (studentDegree == null)
                return ApplicationRankStatusEnum.Under_Qualified;

            if (_jobDegreeFilters.Count == 0)
                return ApplicationRankStatusEnum.Highly_Qualified;

            bool isSatisfied = _jobDegreeFilters.All(filter => filter.IsSatisfiedBy(studentDegree));
            return isSatisfied ? ApplicationRankStatusEnum.Highly_Qualified : ApplicationRankStatusEnum.Under_Qualified;
        }

        public void Close()
        {
            Status = JobStatusEnum.Closed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Pause()
        {
            Status = JobStatusEnum.Paused;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            Status = JobStatusEnum.Active;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
