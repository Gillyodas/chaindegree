using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Reputation.Application.Abstractions;
using ChainDegree.Reputation.Application.Commands.ApplyReputationPenalty;
using ChainDegree.Reputation.Domain;
using ChainDegree.Reputation.Domain.Enums;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Reputation;

public class ReputationIntegrationTests
{
    [Fact]
    public async Task EventDrivenWorkflow_MultipleEvents_CalculatesFinalScoreAndFreezesCorrectly()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var reputationScore = ReputationScore.Create(universityId, 1000);

        var mockRepo = new Mock<IReputationRepository>();
        mockRepo.Setup(r => r.GetByUniversityIdWithHistoriesAsync(universityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reputationScore);

        var handler = new ApplyReputationPenaltyCommandHandler(mockRepo.Object);

        // 1. Minor Penalty Event (S01: -20)
        var event1 = Guid.NewGuid();
        var result1 = await handler.Handle(new ApplyReputationPenaltyCommand(universityId, event1, PenaltyReasonEnum.S01_IdentityInformationError), CancellationToken.None);
        Assert.True(result1.IsSuccess);
        Assert.Equal(980, result1.Value.NewScore);

        // 2. Major Penalty Event (R02: -150)
        var event2 = Guid.NewGuid();
        var result2 = await handler.Handle(new ApplyReputationPenaltyCommand(universityId, event2, PenaltyReasonEnum.R02_OutdatedCurriculum), CancellationToken.None);
        Assert.True(result2.IsSuccess);
        Assert.Equal(830, result2.Value.NewScore);
        Assert.False(reputationScore.IsFrozen);

        // 3. Exemption Shortcut Event (0 penalty)
        var event3 = Guid.NewGuid();
        var result3 = await handler.Handle(new ApplyReputationPenaltyCommand(universityId, event3, PenaltyReasonEnum.Shortcut_Exemption), CancellationToken.None);
        Assert.True(result3.IsSuccess);
        Assert.Equal(830, result3.Value.NewScore);

        // 4. Critical Fraudulent Data Event (R01: -400 + Freeze)
        var event4 = Guid.NewGuid();
        var result4 = await handler.Handle(new ApplyReputationPenaltyCommand(universityId, event4, PenaltyReasonEnum.R01_FraudulentData), CancellationToken.None);
        Assert.True(result4.IsSuccess);
        Assert.Equal(430, result4.Value.NewScore);
        Assert.True(reputationScore.IsFrozen);

        // Verify total history entries recorded
        Assert.Equal(4, reputationScore.Histories.Count);
    }
}
