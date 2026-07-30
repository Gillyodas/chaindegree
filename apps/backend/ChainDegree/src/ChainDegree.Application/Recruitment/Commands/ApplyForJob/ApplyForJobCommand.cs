using System;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Recruitment.Commands.ApplyForJob
{
    public record ApplyForJobCommand(
        Guid JobId,
        Guid DegreeId,
        bool ForceSubmit = false
    ) : IRequest<Result<ApplyForJobResponse>>;

    public record ApplyForJobResponse(
        Guid ApplicationId,
        Guid JobId,
        Guid StudentId,
        string RankStatus,
        string ProcessStatus,
        bool IsForceSubmitted
    );
}
