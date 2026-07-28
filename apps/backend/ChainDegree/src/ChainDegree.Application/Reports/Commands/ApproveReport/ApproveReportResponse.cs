using System;
using System.Collections.Generic;

namespace ChainDegree.Core.Application.Reports.Commands.ApproveReport
{
    public record ApproveReportResponse(
        string Message,
        Guid ReportId,
        IReadOnlyCollection<string> InitiatedProcesses,
        DateTime Timestamp
    );
}
