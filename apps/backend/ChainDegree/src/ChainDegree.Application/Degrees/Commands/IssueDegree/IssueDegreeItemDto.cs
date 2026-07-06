using System;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public sealed record IssueDegreeItemDto(
        Guid StudentId,
        string Major,
        string Classification,
        DateTime IssuedAt
    );
}
