using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Reports.Queries.GetEvidence
{
    public record GetEvidenceQuery(Guid ReportId) : IRequest<Result<GetEvidenceResponse>>;
}
