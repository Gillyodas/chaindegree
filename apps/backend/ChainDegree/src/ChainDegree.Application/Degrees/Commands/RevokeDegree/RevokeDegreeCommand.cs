using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Commands.RevokeDegree
{
    public sealed record RevokeDegreeCommand(
        Guid DegreeId,
        string ReasonCode
    ) : IRequest<Result<RevokeDegreeResponse>>;
}
