using System;
using System.Collections.Generic;
using ChainDegree.Core.Domain.Recruiters.Entities;
using ChainDegree.Core.Domain.Recruiters.Enums;
using ChainDegree.Core.Domain.Recruiters.Events;
using ChainDegree.Core.Domain.SharedKernel;

namespace ChainDegree.Core.Domain.Recruiters
{
    public class Company : AggregateRoot
    {
        public string CompanyName { get; private set; } = null!;
        public string BusinessLicenseCode { get; private set; } = null!;
        public string ContactEmail { get; private set; } = null!;
        public bool IsVerified { get; private set; }
        public CompanyStatusEnum CompanyStatus { get; private set; }

        private readonly List<RecruiterAgent> _recruiterAgents = new();
        public IReadOnlyCollection<RecruiterAgent> RecruiterAgents => _recruiterAgents.AsReadOnly();

        public void VerifyBusiness()
        {
            throw new NotImplementedException();
        }

        public void Deactivate()
        {
            this.IsVerified = false;
            this.CompanyStatus = CompanyStatusEnum.Deactivated;
            this.UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new CompanyDeactivatedEvent(this.Id));
        }
    }
}
