using System;

namespace ChainDegree.Core.Application.Degrees.Queries.DTOs
{
    public sealed record DegreeListDto(
        Guid Id,
        string DegreeCode,
        Guid StudentId,
        string StudentFullName,
        string Major,
        string Classification,
        string Status,
        DateTime IssuedAt,
        string? TxHashBlockchain
    );
}
