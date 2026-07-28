using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reports.Commands.ApproveReport
{
    public record ApproveReportCommand(Guid ReportId) : IRequest<Result<ApproveReportResponse>>;
}
