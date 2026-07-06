using System;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.GetBatchStatus
{
    public sealed record GetBatchStatusQuery(Guid BatchId) : IRequest<Result<BatchQueryResponse>>;
}
