using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Reports.Enums;

namespace ChainDegree.Core.Domain.Reports
{
    public class Report
    {
        public Guid Id { get; private set; }
        public Guid TargetDegreeId { get; private set; }
        public Guid ReporterId { get; private set; }
        public string ReporterRole { get; private set; } = null!; // Student | Recruiter
        public ReportTypeEnum ReportType { get; private set; }
        public string Description { get; private set; } = null!;
        public string? EvidenceFileUrl { get; private set; }
        public ReportStatusEnum Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ReviewedAt { get; private set; }

        public void Approve()
        {
            // Hàm này sẽ sinh và publish FraudulentDataDetectedEvent ra hàng đợi
            throw new NotImplementedException();
        }

        public void Reject()
        {
            throw new NotImplementedException();
        }
    }
}
