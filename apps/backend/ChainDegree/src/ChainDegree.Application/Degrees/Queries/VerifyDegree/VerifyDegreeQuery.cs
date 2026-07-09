using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.VerifyDegree
{
    public sealed record VerifyDegreeQuery(
        string DegreeCode,
        int? Version = null,
        DateTime? IssuedAt = null) : IRequest<Result<VerifyDegreeResponse>>;
}
