using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Degrees.Commands.RetryDegreeConfirmation
{
    public sealed record RetryDegreeConfirmationCommand(Guid DegreeId) : IRequest<Result>;
}
