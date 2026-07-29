using System;
using System.Security.Cryptography;
using System.Text;
using ChainDegree.Core.Domain.Reputation.Enums;
using ChainDegree.Core.Domain.SharedKernel;

namespace ChainDegree.Core.Domain.Reputation;

public class ReputationHistory : Entity
{
    public Guid ReputationScoreId { get; private set; }
    public Guid UniversityId { get; private set; }
    public Guid EventId { get; private set; }
    public int ScoreChange { get; private set; }
    public int NewScore { get; private set; }
    public PenaltyReasonEnum ReasonCode { get; private set; }
    public string? Description { get; private set; }
    public AnchorStatusEnum AnchorStatus { get; private set; }
    public string HistoryHash { get; private set; } = null!;
    public string? TxHash { get; private set; }
    public DateTime Timestamp { get; private set; }

    private ReputationHistory() { }

    public static ReputationHistory Create(
        Guid reputationScoreId,
        Guid universityId,
        Guid eventId,
        int scoreChange,
        int newScore,
        PenaltyReasonEnum reasonCode,
        string? description = null)
    {
        if (reputationScoreId == Guid.Empty)
            throw new ArgumentException("ReputationScoreId cannot be empty.", nameof(reputationScoreId));
        if (universityId == Guid.Empty)
            throw new ArgumentException("UniversityId cannot be empty.", nameof(universityId));
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId cannot be empty.", nameof(eventId));

        var history = new ReputationHistory
        {
            Id = Guid.NewGuid(),
            ReputationScoreId = reputationScoreId,
            UniversityId = universityId,
            EventId = eventId,
            ScoreChange = scoreChange,
            NewScore = newScore,
            ReasonCode = reasonCode,
            Description = description?.Trim(),
            AnchorStatus = AnchorStatusEnum.PendingAnchor,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        history.HistoryHash = ComputeHash(history);
        return history;
    }

    public void MarkAsAnchored(string txHash)
    {
        if (string.IsNullOrWhiteSpace(txHash))
            throw new ArgumentException("TxHash cannot be empty.", nameof(txHash));

        AnchorStatus = AnchorStatusEnum.Anchored;
        TxHash = txHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAnchorFailed()
    {
        AnchorStatus = AnchorStatusEnum.AnchorFailed;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string ComputeHash(ReputationHistory history)
    {
        var rawString = $"{history.UniversityId}:{history.EventId}:{history.ScoreChange}:{history.NewScore}:{history.ReasonCode}:{history.Timestamp:O}";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawString));
        return Convert.ToHexStringLower(bytes);
    }
}
