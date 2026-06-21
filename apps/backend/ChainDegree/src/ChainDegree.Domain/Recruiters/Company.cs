using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Recruiters.Entities;

namespace ChainDegree.Core.Domain.Recruiters
{
    public class Company
    {
        public Guid Id { get; private set; }
        public string CompanyName { get; private set; } = null!;
        public string BusinessLicenseCode { get; private set; } = null!;
        public string ContactEmail { get; private set; } = null!;
        public bool IsVerified { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<RecruiterAgent> _recruiterAgents = new();
        public IReadOnlyCollection<RecruiterAgent> RecruiterAgents => _recruiterAgents.AsReadOnly();

        private readonly List<Job> _jobs = new();
        public IReadOnlyCollection<Job> Jobs => _jobs.AsReadOnly();

        public void VerifyBusiness()
        {
            throw new NotImplementedException();
        }
    }
}
