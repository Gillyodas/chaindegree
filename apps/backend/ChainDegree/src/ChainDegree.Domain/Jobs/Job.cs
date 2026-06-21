using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Applications.Enums;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Jobs.Entities;
using ChainDegree.Core.Domain.Jobs.Enums;

namespace ChainDegree.Core.Domain.Jobs
{
    public class Job
    {
        public Guid Id { get; private set; }
        public Guid CompanyId { get; private set; }
        public Guid CreatedByAgentId { get; private set; }
        public Guid? PartnerUniversityId { get; private set; }
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public decimal SalaryMin { get; private set; }
        public decimal SalaryMax { get; private set; }
        public JobStatusEnum Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<JobDegreeFilter> _jobDegreeFilters = new();
        public IReadOnlyCollection<JobDegreeFilter> JobDegreeFilters => _jobDegreeFilters.AsReadOnly();

        private readonly List<Application> _applications = new();
        public IReadOnlyCollection<Application> Applications => _applications.AsReadOnly();

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
