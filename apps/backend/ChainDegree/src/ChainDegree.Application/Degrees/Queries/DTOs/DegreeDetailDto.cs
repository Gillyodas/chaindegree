using System;

namespace ChainDegree.Core.Application.Degrees.Queries.DTOs
{
    public sealed record DegreeDetailDto(
        Guid Id,
        string DegreeCode,
        Guid InstitutionId,
        Guid SignedByRegistrarId,
        Guid StudentId,
        string StudentFullName,
        string Major,
        string Classification,
        string Status,
        DateTime IssuedAt,
        string? TxHashBlockchain,
        int CurrentVersion,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
