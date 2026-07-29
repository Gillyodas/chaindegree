using System;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Domain.Reputation;
using ChainDegree.Core.Domain.Reputation.Enums;
using ChainDegree.Core.Infrastructure.Blockchain;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ChainDegree.Infrastructure.Tests.Reputation;

public class ReputationInfrastructureTests
{
    private ChainDegreeDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ChainDegreeDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var mockAccessor = new Mock<ICurrentUserAccessor>();
        return new ChainDegreeDbContext(options, mockAccessor.Object, NullLogger<ChainDegreeDbContext>.Instance);
    }

    [Fact]
    public async Task ReputationRepository_AddAndGetByUniversityId_SavesAndRetrievesScore()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("ReputationDb_AddGet");
        var repo = new ReputationRepository(context);
        var universityId = Guid.NewGuid();
        var score = ReputationScore.Create(universityId, 1000);
        score.ApplyPenalty(Guid.NewGuid(), PenaltyReasonEnum.S01_IdentityInformationError, "Minor penalty");

        // Act
        await repo.AddAsync(score);
        await context.SaveChangesAsync();

        var retrieved = await repo.GetByUniversityIdWithHistoriesAsync(universityId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(universityId, retrieved.UniversityId);
        Assert.Equal(980, retrieved.CurrentScore);
        Assert.Single(retrieved.Histories);
    }

    [Fact]
    public async Task ReputationRepository_HasEventBeenProcessedAsync_DetectsExistingEvents()
    {
        // Arrange
        using var context = CreateInMemoryDbContext("ReputationDb_EventCheck");
        var repo = new ReputationRepository(context);
        var universityId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var score = ReputationScore.Create(universityId);
        score.ApplyPenalty(eventId, PenaltyReasonEnum.S02_AcademicResultError);

        await repo.AddAsync(score);
        await context.SaveChangesAsync();

        // Act
        var hasBeenProcessed = await repo.HasEventBeenProcessedAsync(eventId);
        var nonExistentEventProcessed = await repo.HasEventBeenProcessedAsync(Guid.NewGuid());

        // Assert
        Assert.True(hasBeenProcessed);
        Assert.False(nonExistentEventProcessed);
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
