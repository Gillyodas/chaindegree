using System;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public sealed record IssueDegreeFailureDto(
        Guid StudentId,
        string Major,
        string Reason
    );
}
