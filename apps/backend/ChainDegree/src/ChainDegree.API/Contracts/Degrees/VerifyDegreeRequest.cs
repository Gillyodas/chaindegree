using System;

namespace ChainDegree.API.Contracts.Degrees
{
    public record VerifyDegreeRequest(
        string DegreeCode,
        int? Version = null,
        DateTime? IssuedAt = null
    );
}
