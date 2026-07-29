using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Reputation;
using ChainDegree.Core.Domain.Reputation.Enums;
using ChainDegree.Core.Infrastructure.Blockchain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ChainDegree.Infrastructure.Tests.Reputation;

public class ReputationInfrastructureTests
{
    [Fact]
    public async Task ReputationRepository_GetByUniversityIdWithHistories_ReturnsMockedScore()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var expectedScore = ReputationScore.Create(universityId, 1000);
        expectedScore.ApplyPenalty(Guid.NewGuid(), PenaltyReasonEnum.S01_IdentityInformationError, "Minor penalty");

        var mockRepo = new Mock<IReputationRepository>();
        mockRepo.Setup(r => r.GetByUniversityIdWithHistoriesAsync(universityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScore);

        // Act
        var result = await mockRepo.Object.GetByUniversityIdWithHistoriesAsync(universityId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(universityId, result.UniversityId);
        Assert.Equal(980, result.CurrentScore);
        Assert.Single(result.Histories);
    }

    [Fact]
    public async Task ReputationRepository_HasEventBeenProcessedAsync_ReturnsMockedResult()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var mockRepo = new Mock<IReputationRepository>();
        mockRepo.Setup(r => r.HasEventBeenProcessedAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var hasBeenProcessed = await mockRepo.Object.HasEventBeenProcessedAsync(eventId);

        // Assert
        Assert.True(hasBeenProcessed);
    }

    [Fact]
    public async Task NethereumReputationBlockchainService_AnchorHistoryHashAsync_ReturnsValidTxHash()
    {
        // Arrange
        var service = new NethereumReputationBlockchainService(NullLogger<NethereumReputationBlockchainService>.Instance);
        var historyId = Guid.NewGuid();
        var historyHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var txHash = await service.AnchorHistoryHashAsync(historyId, historyHash);

        // Assert
        Assert.NotNull(txHash);
        Assert.StartsWith("0x", txHash);
        Assert.Equal(66, txHash.Length);
    }
}
