using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reports.Commands.RejectReport
{
    public record RejectReportCommand(Guid ReportId, string Reason) : IRequest<Result<RejectReportResponse>>;
}
