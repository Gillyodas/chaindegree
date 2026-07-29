using System;
using System.Collections.Generic;

namespace ChainDegree.Reputation.Application.Queries.GetReputationHistory;

public record ReputationHistoryItemDto(
    Guid Id,
    Guid EventId,
    int ScoreChange,
    int NewScore,
    string ReasonCode,
    string? Description,
    string AnchorStatus,
    string? HistoryHash,
    string? TxHash,
    DateTime Timestamp);

public record ReputationHistoryResponse(
    Guid UniversityId,
    int TotalCount,
    IReadOnlyList<ReputationHistoryItemDto> Items);
