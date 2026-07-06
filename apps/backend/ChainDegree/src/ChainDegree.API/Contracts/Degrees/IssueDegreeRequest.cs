using System;
using System.Collections.Generic;

namespace ChainDegree.API.Contracts.Degrees
{
    public sealed record IssueDegreeRequest(List<IssueDegreeItemRequest> Degrees);

    public sealed record IssueDegreeItemRequest(
        Guid StudentId,
        string Major,
        string Classification,
        DateTime IssuedAt
    );
}
