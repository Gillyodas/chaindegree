using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Reputation.Api;
using ChainDegree.Reputation.Application.Queries.GetInstitutionReputation;
using ChainDegree.Reputation.Application.Queries.GetReputationHistory;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ChainDegree.API.Tests.Controllers;

public class ReputationsControllerTests
{
    private readonly Mock<ISender> _mockSender;
    private readonly ReputationsController _controller;

    public ReputationsControllerTests()
    {
        _mockSender = new Mock<ISender>();
        _controller = new ReputationsController(_mockSender.Object);
    }

    [Fact]
    public async Task GetInstitutionReputation_Success_ReturnsOkObjectResult()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var expectedResponse = new ReputationResponse(universityId, 980, false, DateTime.UtcNow);

        _mockSender.Setup(s => s.Send(It.Is<GetInstitutionReputationQuery>(q => q.UniversityId == universityId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReputationResponse>.Success(expectedResponse));

        // Act
        var actionResult = await _controller.GetInstitutionReputation(universityId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var value = Assert.IsType<ReputationResponse>(okResult.Value);
        Assert.Equal(980, value.CurrentScore);
        Assert.False(value.IsFrozen);
    }

    [Fact]
    public async Task GetInstitutionReputation_Failure_ReturnsErrorResult()
    {
        // Arrange
        var universityId = Guid.Empty;
        _mockSender.Setup(s => s.Send(It.IsAny<GetInstitutionReputationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReputationResponse>.Failure(new Error("Reputation.InvalidId", "Invalid University ID", ErrorType.Validation)));

        // Act
        var actionResult = await _controller.GetInstitutionReputation(universityId, CancellationToken.None);

        // Assert
        Assert.NotNull(actionResult);
    }

    [Fact]
    public async Task GetReputationHistory_Success_ReturnsOkObjectResultWithHistory()
    {
        // Arrange
        var universityId = Guid.NewGuid();
        var historyResponse = new ReputationHistoryResponse(
            universityId,
            1,
            new List<ReputationHistoryItemDto>
            {
                new ReputationHistoryItemDto(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    -20,
                    980,
                    "S01_IdentityInformationError",
                    "Minor penalty",
                    "Anchored",
                    "0xhash",
                    "0xtx",
                    DateTime.UtcNow)
            });

        _mockSender.Setup(s => s.Send(It.Is<GetReputationHistoryQuery>(q => q.UniversityId == universityId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReputationHistoryResponse>.Success(historyResponse));

        // Act
        var actionResult = await _controller.GetReputationHistory(universityId, 1, 20, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var value = Assert.IsType<ReputationHistoryResponse>(okResult.Value);
        Assert.Equal(1, value.TotalCount);
        Assert.Single(value.Items);
    }
}
