using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Reports;

namespace ChainDegree.Core.Domain.Degrees
{
    public class Degree
    {
        public Guid Id { get; private set; }
        public string DegreeCode { get; private set; } = null!;
        public Guid InstitutionId { get; private set; }
        public Guid SignedByRegistrarId { get; private set; }
        public Guid StudentId { get; private set; }
        public string Major { get; private set; } = null!;
        public string Classification { get; private set; } = null!;
        public CryptoSnapshot CryptoData { get; private set; } = null!;
        public StatusEnum Status { get; private set; }
        public string? TxHashBlockchain { get; private set; }
        public DateTime IssuedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<Report> _reports = new();
        public IReadOnlyCollection<Report> Reports => _reports.AsReadOnly();

        private readonly List<Application> _applications = new();
        public IReadOnlyCollection<Application> Applications => _applications.AsReadOnly();

        public void ConfirmBlockchainSync(string txHash)
        {
            throw new NotImplementedException();
        }

        public void InitiateRevocation()
        {
            throw new NotImplementedException();
        }

        public void Revoke()
        {
            throw new NotImplementedException();
        }
    }
}
