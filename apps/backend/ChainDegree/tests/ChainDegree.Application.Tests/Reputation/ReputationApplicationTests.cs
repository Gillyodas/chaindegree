using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Reputation.Application.Abstractions;
using ChainDegree.Reputation.Application.Commands.ApplyReputationPenalty;
using ChainDegree.Reputation.Application.Queries.GetInstitutionReputation;
using ChainDegree.Reputation.Application.Queries.GetReputationHistory;
using ChainDegree.Reputation.Domain;
using ChainDegree.Reputation.Domain.Enums;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Reputation;

public class ReputationApplicationTests
{
    private readonly Mock<IReputationRepository> _mockRepo;

    public ReputationApplicationTests()
    {
        _mockRepo = new Mock<IReputationRepository>();
    }

    [Fact]
    public async Task GetInstitutionReputation_NonExistingScore_ReturnsDefault1000Score()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByUniversityIdAsync(universityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReputationScore?)null);

        var handler = new GetInstitutionReputationQueryHandler(_mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetInstitutionReputationQuery(universityId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.Value.CurrentScore);
        Assert.False(result.Value.IsFrozen);
    }

    [Fact]
    public async Task GetInstitutionReputation_ExistingScore_ReturnsScoreDetails()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var score = ReputationScore.Create(universityId);
        score.ApplyPenalty(Guid.NewGuid(), PenaltyReasonEnum.S01_IdentityInformationError);

        _mockRepo.Setup(r => r.GetByUniversityIdAsync(universityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);

        var handler = new GetInstitutionReputationQueryHandler(_mockRepo.Object);

        // Act
        var result = await handler.Handle(new GetInstitutionReputationQuery(universityId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(980, result.Value.CurrentScore);
    }

    [Fact]
    public async Task ApplyReputationPenalty_NewScoreRecord_CreatesScoreAndAppliesPenalty()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _mockRepo.Setup(r => r.HasEventBeenProcessedAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepo.Setup(r => r.GetByUniversityIdWithHistoriesAsync(universityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReputationScore?)null);

        var handler = new ApplyReputationPenaltyCommandHandler(_mockRepo.Object);
        var command = new ApplyReputationPenaltyCommand(universityId, eventId, PenaltyReasonEnum.R02_OutdatedCurriculum, "Curriculum issue");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(-150, result.Value.ScoreChange);
        Assert.Equal(850, result.Value.NewScore);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<ReputationScore>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.Update(It.IsAny<ReputationScore>()), Times.Once);
    }

    [Fact]
    public async Task ApplyReputationPenalty_AlreadyProcessedEventId_ReturnsExistingHistoryWithoutDeductingTwice()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var score = ReputationScore.Create(universityId);
        score.ApplyPenalty(eventId, PenaltyReasonEnum.S01_IdentityInformationError);

        _mockRepo.Setup(r => r.HasEventBeenProcessedAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepo.Setup(r => r.GetByUniversityIdWithHistoriesAsync(universityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);

        var handler = new ApplyReputationPenaltyCommandHandler(_mockRepo.Object);
        var command = new ApplyReputationPenaltyCommand(universityId, eventId, PenaltyReasonEnum.S01_IdentityInformationError);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(980, result.Value.NewScore);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<ReputationScore>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetReputationHistory_ReturnsPaginatedHistoryItems()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var score = ReputationScore.Create(universityId);
        score.ApplyPenalty(Guid.NewGuid(), PenaltyReasonEnum.S01_IdentityInformationError);
        score.ApplyPenalty(Guid.NewGuid(), PenaltyReasonEnum.R02_OutdatedCurriculum);

        _mockRepo.Setup(r => r.GetByUniversityIdWithHistoriesAsync(universityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);

        var handler = new GetReputationHistoryQueryHandler(_mockRepo.Object);
        var query = new GetReputationHistoryQuery(universityId, Page: 1, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
    }
}
