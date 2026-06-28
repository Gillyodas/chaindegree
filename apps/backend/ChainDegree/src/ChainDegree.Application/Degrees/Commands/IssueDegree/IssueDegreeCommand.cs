using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public sealed record IssueDegreeCommand(
        Guid InstitutionId,
        Guid SignedByRegistrarId,
        Guid StudentId,
        string Major,
        string Classification
    ) : IRequest<Result<Degree>>;
}
