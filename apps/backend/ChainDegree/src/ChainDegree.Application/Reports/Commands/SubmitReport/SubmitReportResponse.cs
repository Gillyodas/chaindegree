using System;

namespace ChainDegree.Core.Application.Reports.Commands.SubmitReport
{
    public record SubmitReportResponse(
        Guid ReportId,
        Guid DegreeId,
        string Status,
        string? EvidenceFileName,
        DateTime CreatedAt
    );
}
