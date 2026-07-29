using System;
using System.Linq;
using ChainDegree.Reputation.Domain;
using ChainDegree.Reputation.Domain.Enums;
using ChainDegree.Reputation.Domain.Events;
using Xunit;

namespace ChainDegree.Domain.Tests.Reputation;

public class ReputationScoreTests
{
    [Fact]
    public void Create_ValidUniversityId_InitializesScoreTo1000AndNotFrozen()
    {
        // Arrange
        var universityId = Guid.NewGuid();

        // Act
        var reputation = ReputationScore.Create(universityId);

        // Assert
        Assert.NotNull(reputation);
        Assert.Equal(universityId, reputation.UniversityId);
        Assert.Equal(1000, reputation.CurrentScore);
        Assert.False(reputation.IsFrozen);
        Assert.Empty(reputation.Histories);
    }

    [Theory]
    [InlineData(PenaltyReasonEnum.S01_IdentityInformationError, 20, false)]
    [InlineData(PenaltyReasonEnum.S02_AcademicResultError, 20, false)]
    [InlineData(PenaltyReasonEnum.R02_OutdatedCurriculum, 150, false)]
    public void ApplyPenalty_MinorOrMajorPenalty_DeductsPointsWithoutFreezing(PenaltyReasonEnum reasonCode, int expectedDeduction, bool expectedFreeze)
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId);
        var eventId = Guid.NewGuid();

        // Act
        var result = reputation.ApplyPenalty(eventId, reasonCode, "Test penalty");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000 - expectedDeduction, reputation.CurrentScore);
        Assert.Equal(expectedFreeze, reputation.IsFrozen);
        Assert.Single(reputation.Histories);

        var history = reputation.Histories.First();
        Assert.Equal(eventId, history.EventId);
        Assert.Equal(-expectedDeduction, history.ScoreChange);
        Assert.Equal(reputation.CurrentScore, history.NewScore);

        var scoreChangedEvent = reputation.DomainEvents.OfType<ReputationScoreChangedEvent>().FirstOrDefault();
        Assert.NotNull(scoreChangedEvent);
        Assert.Equal(1000, scoreChangedEvent.OldScore);
        Assert.Equal(reputation.CurrentScore, scoreChangedEvent.NewScore);
    }

    [Fact]
    public void ApplyPenalty_R01FraudulentData_Deducts400PointsAndFreezesInstitution()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId);
        var eventId = Guid.NewGuid();

        // Act
        var result = reputation.ApplyPenalty(eventId, PenaltyReasonEnum.R01_FraudulentData, "Severe fraudulent data");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(600, reputation.CurrentScore);
        Assert.True(reputation.IsFrozen);

        var frozenEvent = reputation.DomainEvents.OfType<InstitutionFrozenEvent>().FirstOrDefault();
        Assert.NotNull(frozenEvent);
        Assert.Equal(universityId, frozenEvent.UniversityId);
        Assert.Equal(PenaltyReasonEnum.R01_FraudulentData, frozenEvent.ReasonCode);
    }

    [Fact]
    public void ApplyPenalty_H01SystemCompromised_Deducts0PointsAndFreezesInstitution()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId);
        var eventId = Guid.NewGuid();

        // Act
        var result = reputation.ApplyPenalty(eventId, PenaltyReasonEnum.H01_SystemCompromised, "System breach detected");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000, reputation.CurrentScore);
        Assert.True(reputation.IsFrozen);
    }

    [Fact]
    public void ApplyPenalty_ScoreDoesNotDropBelowZero()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId, initialScore: 100);
        var eventId = Guid.NewGuid();

        // Act (R01 deducts 400, but score is 100)
        var result = reputation.ApplyPenalty(eventId, PenaltyReasonEnum.R01_FraudulentData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, reputation.CurrentScore);
    }

    [Fact]
    public void ApplyPenalty_DuplicateEventId_IsIdempotentAndDoesNotDeductTwice()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId);
        var eventId = Guid.NewGuid();

        // Act
        var result1 = reputation.ApplyPenalty(eventId, PenaltyReasonEnum.S01_IdentityInformationError);
        var result2 = reputation.ApplyPenalty(eventId, PenaltyReasonEnum.S01_IdentityInformationError);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(980, reputation.CurrentScore);
        Assert.Single(reputation.Histories);
    }

    [Fact]
    public void ApplyExemption_Deducts0PointsAndRecordsShortcutReason()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId);
        var eventId = Guid.NewGuid();

        // Act
        var result = reputation.ApplyExemption(eventId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000, reputation.CurrentScore);
        Assert.False(reputation.IsFrozen);
        Assert.Single(reputation.Histories);
        Assert.Equal(PenaltyReasonEnum.Shortcut_Exemption, reputation.Histories.First().ReasonCode);
    }

    [Fact]
    public void Freeze_AlreadyFrozen_ReturnsConflictError()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId);
        reputation.Freeze(PenaltyReasonEnum.H01_SystemCompromised, "Initial freeze");

        // Act
        var result = reputation.Freeze(PenaltyReasonEnum.H01_SystemCompromised, "Second freeze");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Reputation.AlreadyFrozen", result.Error.Code);
    }

    [Fact]
    public void Unfreeze_NotFrozen_ReturnsConflictError()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputation = ReputationScore.Create(universityId);

        // Act
        var result = reputation.Unfreeze();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Reputation.NotFrozen", result.Error.Code);
    }

    [Fact]
    public void ReputationHistory_ComputesHashAndAllowsMarkAsAnchored()
    {
        // Arrange
        var scoreId = Guid.NewGuid();
        var universityId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Act
        var history = ReputationHistory.Create(scoreId, universityId, eventId, -20, 980, PenaltyReasonEnum.S01_IdentityInformationError, "Test");

        // Assert
        Assert.NotNull(history.HistoryHash);
        Assert.NotEmpty(history.HistoryHash);
        Assert.Equal(AnchorStatusEnum.PendingAnchor, history.AnchorStatus);

        history.MarkAsAnchored("0x123abc456def");
        Assert.Equal(AnchorStatusEnum.Anchored, history.AnchorStatus);
        Assert.Equal("0x123abc456def", history.TxHash);
    }
}
