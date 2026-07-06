using System;
using System.Collections.Generic;

namespace ChainDegree.Core.Application.Degrees.Commands.IssueDegree
{
    public sealed record IssueDegreeResponse(
        string Message,
        int AcceptedCount,
        IReadOnlyList<Guid> DegreeIds,
        IReadOnlyList<IssueDegreeFailureDto> Failures
    );
}
