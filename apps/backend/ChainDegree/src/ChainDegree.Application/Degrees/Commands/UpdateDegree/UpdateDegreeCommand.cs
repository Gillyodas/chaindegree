using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Commands.UpdateDegree
{
    public sealed record UpdateDegreeCommand(
        Guid DegreeId,
        string Major,
        string Classification,
        string ReasonCode
    ) : IRequest<Result<UpdateDegreeResponse>>;
}
