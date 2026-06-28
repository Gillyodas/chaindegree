using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public class IssueDegreeCommandHandler : IRequestHandler<IssueDegreeCommand, Result<Degree>>
    {
        public Task<Result<Degree>> Handle(IssueDegreeCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
