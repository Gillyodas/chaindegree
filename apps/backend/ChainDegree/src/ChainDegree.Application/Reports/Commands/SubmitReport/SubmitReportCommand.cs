using System;
using System.IO;
using ChainDegree.Core.Domain.Reports.Enums;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reports.Commands.SubmitReport
{
    public record SubmitReportCommand(
        Guid TargetDegreeId,
        ReportTypeEnum ReportType,
        string Description,
        Stream EvidenceStream,
        string ContentType,
        string FileName
    ) : IRequest<Result<SubmitReportResponse>>;
}
