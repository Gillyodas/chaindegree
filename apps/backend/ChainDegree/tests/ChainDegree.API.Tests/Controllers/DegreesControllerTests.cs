using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.API.Contracts.Degrees;
using ChainDegree.API.Controllers;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Application.Degrees.Commands.RetryDegreeConfirmation;
using ChainDegree.Core.Application.Degrees.Queries.GetBatchStatus;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ChainDegree.API.Tests.Controllers
{
    public class DegreesControllerTests
    {
        private readonly Mock<ISender> _mockSender;
        private readonly DegreesController _controller;

        public DegreesControllerTests()
        {
            _mockSender = new Mock<ISender>();
            _controller = new DegreesController(_mockSender.Object);
        }

        [Fact]
        public async Task IssueDegrees_WithValidRequest_ReturnsAccepted()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var request = new IssueDegreeRequest(new List<IssueDegreeItemRequest>
            {
                new(studentId, "Software Engineering", "Giỏi", DateTime.UtcNow)
            });

            var response = new IssueDegreeResponse(
                "Request accepted",
                1,
                new List<Guid> { Guid.NewGuid() },
                new List<IssueDegreeFailureDto>()
            );

            _mockSender.Setup(s => s.Send(It.IsAny<IssueDegreeCommand>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<IssueDegreeResponse>.Success(response));

            // Act
            var result = await _controller.IssueDegrees(request, CancellationToken.None);

            // Assert
            var acceptedResult = Assert.IsType<AcceptedResult>(result);
            var returnedValue = Assert.IsType<IssueDegreeResponse>(acceptedResult.Value);
            Assert.Equal(1, returnedValue.AcceptedCount);
            Assert.Empty(returnedValue.Failures);
        }

        [Fact]
        public async Task GetBatchStatus_WithValidBatchId_ReturnsOk()
        {
            // Arrange
            var batchId = Guid.NewGuid();
            var queryResponse = new BatchQueryResponse(
                batchId,
                "BATCH_UIT_123",
                "Completed",
                1,
                "merkle_root",
                "tx_hash",
                100L,
                180,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow
            );

            _mockSender.Setup(s => s.Send(It.IsAny<GetBatchStatusQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<BatchQueryResponse>.Success(queryResponse));

            // Act
            var result = await _controller.GetBatchStatus(batchId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = Assert.IsType<BatchQueryResponse>(okResult.Value);
            Assert.Equal(batchId, returnedValue.BatchId);
            Assert.Equal("Completed", returnedValue.Status);
        }

        [Fact]
        public async Task RetryDegreeConfirmation_WithValidId_ReturnsAccepted()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            _mockSender.Setup(s => s.Send(It.IsAny<RetryDegreeConfirmationCommand>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.RetryDegreeConfirmation(degreeId, CancellationToken.None);

            // Assert
            Assert.IsType<AcceptedResult>(result);
        }
    }
}
