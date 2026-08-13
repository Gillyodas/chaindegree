using System;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Queries.GetDegreeById
{
    public record GetDegreeByIdQuery(Guid Id) : IRequest<Result<DegreeDetailDto>>;
}
