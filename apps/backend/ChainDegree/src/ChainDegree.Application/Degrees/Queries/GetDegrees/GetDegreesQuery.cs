using System;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.GetDegrees
{
    public record GetDegreesQuery(int PageIndex = 1, int PageSize = 20) : IRequest<Result<PagedResult<DegreeListDto>>>;
}
