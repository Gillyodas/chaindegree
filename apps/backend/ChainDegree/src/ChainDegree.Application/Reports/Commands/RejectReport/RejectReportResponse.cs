using System;

namespace ChainDegree.Core.Application.Reports.Commands.RejectReport
{
    public record RejectReportResponse(
        string Message,
        Guid ReportId,
        string Reason,
        DateTime Timestamp
    );
}
