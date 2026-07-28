using System;
using ChainDegree.Core.Domain.Reports.Enums;
using Microsoft.AspNetCore.Http;

namespace ChainDegree.API.Contracts.Reports
{
    public class SubmitReportRequest
    {
        public Guid DegreeId { get; set; }
        public ReportTypeEnum ReportType { get; set; }
        public string Description { get; set; } = null!;
        public IFormFile EvidenceFile { get; set; } = null!;
    }
}
