using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public sealed record IssueDegreeCommand(
        List<IssueDegreeItemDto> Degrees
    ) : IRequest<Result<IssueDegreeResponse>>;
}
