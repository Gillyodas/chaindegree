using System;
using System.Collections.Generic;
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

        public bool IsExpired => DateTime.UtcNow > ApplicationEndDate;

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
            JobStatusEnum status)
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
            CreatedAt = DateTime.UtcNow;
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
            DateTime applicationEndDate)
        {
            if (companyId == Guid.Empty || createdByAgentId == Guid.Empty)
                return Result<Job>.Failure(JobErrors.EmptyIdentifiers);

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
                return Result<Job>.Failure(JobErrors.MissingJobDetails);

            if (salaryMin < 0 || salaryMax < salaryMin)
                return Result<Job>.Failure(JobErrors.InvalidSalaryRange);

            DateTime actualStartDate = applicationStartDate ?? DateTime.UtcNow;

            if (actualStartDate >= applicationEndDate)
                return Result<Job>.Failure(JobErrors.InvalidDateRange);

            if (applicationEndDate <= DateTime.UtcNow)
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
                JobStatusEnum.Draft
            );

            return Result<Job>.Success(job);
        }

        public void AddFilter(DegreeTypeEnum degreeType, string major, string minClassification)
        {
            throw new NotImplementedException();
        }

        public ApplicationRankStatusEnum EvaluateApplication(Degree studentDegree)
        {
            throw new NotImplementedException();
        }
    }
}
