using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.API.Contracts.Degrees;
using ChainDegree.API.Controllers;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Application.Degrees.Commands.RetryDegreeConfirmation;
using ChainDegree.Core.Application.Degrees.Queries.GetBatchStatus;
using ChainDegree.Core.Application.Degrees.Commands.RevokeDegree;
using ChainDegree.Core.Application.Degrees.Commands.UpdateDegree;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.Core.Application.Degrees.Queries.GetDegrees;
using ChainDegree.Core.Application.Degrees.Queries.GetDegreeById;
using ChainDegree.Core.Application.Degrees.Queries.VerifyDegree;
using ChainDegree.Core.Application.Degrees.Queries.ListDegreeVersions;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
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
        public async Task GetDegrees_WithValidQuery_ReturnsOk()
        {
            // Arrange
            var pagedResult = new PagedResult<DegreeListDto>(
                new List<DegreeListDto>
                {
                    new(Guid.NewGuid(), "DEG-001", Guid.NewGuid(), "Student A", "SE", "Gioi", "Confirmed", DateTime.UtcNow, "0x1")
                },
                totalCount: 1,
                pageIndex: 1,
                pageSize: 20
            );

            _mockSender.Setup(s => s.Send(It.IsAny<GetDegreesQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<PagedResult<DegreeListDto>>.Success(pagedResult));

            // Act
            var result = await _controller.GetDegrees(1, 20, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = Assert.IsType<PagedResult<DegreeListDto>>(okResult.Value);
            Assert.Equal(1, returnedValue.TotalCount);
        }

        [Fact]
        public async Task GetDegreeById_WithValidId_ReturnsOk()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            var detailDto = new DegreeDetailDto(
                degreeId,
                "DEG-002",
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Student B",
                "CS",
                "Xuat Sac",
                "Confirmed",
                DateTime.UtcNow,
                "0x2",
                1,
                DateTime.UtcNow,
                null
            );

            _mockSender.Setup(s => s.Send(It.IsAny<GetDegreeByIdQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<DegreeDetailDto>.Success(detailDto));

            // Act
            var result = await _controller.GetDegreeById(degreeId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = Assert.IsType<DegreeDetailDto>(okResult.Value);
            Assert.Equal("DEG-002", returnedValue.DegreeCode);
        }

        [Fact]
        public async Task GetDegreeById_WithNotFoundOrCrossTenant_ReturnsNotFound()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            _mockSender.Setup(s => s.Send(It.IsAny<GetDegreeByIdQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<DegreeDetailDto>.Failure(DegreeErrors.NotFound));

            // Act
            var result = await _controller.GetDegreeById(degreeId, CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
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

        [Fact]
        public async Task RevokeDegree_WithShortcut_ReturnsOk()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            var request = new RevokeDegreeRequest("R-01");
            var response = new RevokeDegreeResponse(degreeId, "Revoked", true, "Shortcut success");

            _mockSender.Setup(s => s.Send(It.IsAny<RevokeDegreeCommand>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<RevokeDegreeResponse>.Success(response));

            // Act
            var result = await _controller.RevokeDegree(degreeId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = Assert.IsType<RevokeDegreeResponse>(okResult.Value);
            Assert.True(returnedValue.IsShortcut);
            Assert.Equal("Revoked", returnedValue.Status);
        }

        [Fact]
        public async Task RevokeDegree_WithNormalFlow_ReturnsAccepted()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            var request = new RevokeDegreeRequest("R-01");
            var response = new RevokeDegreeResponse(degreeId, "Pending_Revocation", false, "Accepted and queued");

            _mockSender.Setup(s => s.Send(It.IsAny<RevokeDegreeCommand>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<RevokeDegreeResponse>.Success(response));

            // Act
            var result = await _controller.RevokeDegree(degreeId, request, CancellationToken.None);

            // Assert
            var acceptedResult = Assert.IsType<AcceptedResult>(result);
            var returnedValue = Assert.IsType<RevokeDegreeResponse>(acceptedResult.Value);
            Assert.False(returnedValue.IsShortcut);
            Assert.Equal("Pending_Revocation", returnedValue.Status);
        }

        [Fact]
        public async Task UpdateDegree_WithShortcut_ReturnsOk()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            var request = new UpdateDegreeRequest("AI", "Xuất sắc", "S-01");
            var response = new UpdateDegreeResponse(degreeId, "Pending_Confirmation", true, "Shortcut success");

            _mockSender.Setup(s => s.Send(It.IsAny<UpdateDegreeCommand>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<UpdateDegreeResponse>.Success(response));

            // Act
            var result = await _controller.UpdateDegree(degreeId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = Assert.IsType<UpdateDegreeResponse>(okResult.Value);
            Assert.True(returnedValue.IsShortcut);
        }

        [Fact]
        public async Task UpdateDegree_WithNormalFlow_ReturnsAccepted()
        {
            // Arrange
            var degreeId = Guid.NewGuid();
            var request = new UpdateDegreeRequest("AI", "Xuất sắc", "S-01");
            var response = new UpdateDegreeResponse(degreeId, "Pending_Update", false, "Accepted and queued");

            _mockSender.Setup(s => s.Send(It.IsAny<UpdateDegreeCommand>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<UpdateDegreeResponse>.Success(response));

            // Act
            var result = await _controller.UpdateDegree(degreeId, request, CancellationToken.None);

            // Assert
            var acceptedResult = Assert.IsType<AcceptedResult>(result);
            var returnedValue = Assert.IsType<UpdateDegreeResponse>(acceptedResult.Value);
            Assert.False(returnedValue.IsShortcut);
        }

        [Fact]
        public async Task VerifyDegree_WithValidSnapshot_ReturnsOk()
        {
            // Arrange
            var request = new VerifyDegreeRequest("DEG-2026-000001", null, null);
            var response = new VerifyDegreeResponse(
                Verified: true,
                Status: "Confirmed",
                VerificationSource: ChainDegree.Core.Domain.Degrees.Enums.VerificationSource.Blockchain_Merkle_Root,
                DegreeCode: "DEG-2026-000001",
                Version: 1,
                InstitutionName: "Test Institution",
                StudentFullName: "Nguyen Van A",
                Major: "IT",
                Classification: "Gioi",
                IssuedAt: DateTime.UtcNow,
                Blockchain: new BlockchainDetails("0x123", 100, "0xabc", "proof")
            );

            _mockSender.Setup(s => s.Send(It.IsAny<VerifyDegreeQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<VerifyDegreeResponse>.Success(response));

            // Act
            var result = await _controller.VerifyDegree(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedValue = Assert.IsType<VerifyDegreeResponse>(okResult.Value);
            Assert.True(returnedValue.Verified);
            Assert.Equal("Confirmed", returnedValue.Status);
        }

        [Fact]
        public async Task VerifyDegree_WithCryptoMismatch_ReturnsUnprocessableEntity()
        {
            // Arrange
            var request = new VerifyDegreeRequest("DEG-2026-000001", null, null);
            _mockSender.Setup(s => s.Send(It.IsAny<VerifyDegreeQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch));

            // Act
            var result = await _controller.VerifyDegree(request, CancellationToken.None);

            // Assert
            var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var err = Assert.IsType<VerifyDegreeErrorResponse>(unprocessableResult.Value);
            Assert.False(err.Verified);
            Assert.Equal("CRYPTO_HASH_MISMATCH", err.ErrorCode);
        }

        [Fact]
        public async Task VerifyDegree_WithBlockchainInvalid_ReturnsUnprocessableEntity()
        {
            // Arrange
            var request = new VerifyDegreeRequest("DEG-2026-000001", null, null);
            _mockSender.Setup(s => s.Send(It.IsAny<VerifyDegreeQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid));

            // Act
            var result = await _controller.VerifyDegree(request, CancellationToken.None);

            // Assert
            var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
            var err = Assert.IsType<VerifyDegreeErrorResponse>(unprocessableResult.Value);
            Assert.False(err.Verified);
            Assert.Equal("BLOCKCHAIN_INVALID", err.ErrorCode);
        }

        [Fact]
        public async Task VerifyDegree_WithNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new VerifyDegreeRequest("DEG-2026-000001", null, null);
            _mockSender.Setup(s => s.Send(It.IsAny<VerifyDegreeQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<VerifyDegreeResponse>.Failure(DegreeErrors.NotFound));

            // Act
            var result = await _controller.VerifyDegree(request, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var err = Assert.IsType<VerifyDegreeErrorResponse>(notFoundResult.Value);
            Assert.False(err.Verified);
            Assert.Equal("DEGREE_NOT_FOUND", err.ErrorCode);
        }

        [Fact]
        public async Task VerifyDegree_WithUnsupportedVersion_ReturnsNotFound()
        {
            // Arrange
            var request = new VerifyDegreeRequest("DEG-2026-000001", 3, null);
            _mockSender.Setup(s => s.Send(It.IsAny<VerifyDegreeQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<VerifyDegreeResponse>.Failure(DegreeErrors.UnsupportedVersion));

            // Act
            var result = await _controller.VerifyDegree(request, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var err = Assert.IsType<VerifyDegreeErrorResponse>(notFoundResult.Value);
            Assert.False(err.Verified);
            Assert.Equal("UNSUPPORTED_VERSION", err.ErrorCode);
        }

        [Fact]
        public async Task VerifyDegree_WithInvalidSaltFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new VerifyDegreeRequest("DEG-2026-000001", null, null, "{}", "bad");
            _mockSender.Setup(s => s.Send(It.IsAny<VerifyDegreeQuery>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<VerifyDegreeResponse>.Failure(DegreeErrors.InvalidSaltFormat));

            // Act
            var result = await _controller.VerifyDegree(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var err = Assert.IsType<VerifyDegreeErrorResponse>(badRequestResult.Value);
            Assert.False(err.Verified);
            Assert.Equal("INVALID_SALT_FORMAT", err.ErrorCode);
        }

        [Fact]
        public async Task ListDegreeVersions_DegreeNotFound_ReturnsNotFound()
        {
            // Arrange
            var degreeCode = "DEG-2026-999999";
            _mockSender.Setup(s => s.Send(It.Is<ListDegreeVersionsQuery>(q => q.DegreeCode == degreeCode), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<DegreeVersionListResponse>.Failure(DegreeErrors.NotFound));

            // Act
            var result = await _controller.ListDegreeVersions(degreeCode, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var err = Assert.IsType<VerifyDegreeErrorResponse>(notFoundResult.Value);
            Assert.False(err.Verified);
            Assert.Equal("DEGREE_NOT_FOUND", err.ErrorCode);
        }

        [Fact]
        public async Task ListDegreeVersions_DegreeFound_ReturnsOkWithVersions()
        {
            // Arrange
            var degreeCode = "DEG-2026-000001";
            var expectedResponse = new DegreeVersionListResponse(
                degreeCode,
                2,
                new List<DegreeVersionItem>
                {
                    new DegreeVersionItem(2, DateTime.UtcNow, true),
                    new DegreeVersionItem(1, DateTime.UtcNow.AddMonths(-1), false)
                });

            _mockSender.Setup(s => s.Send(It.Is<ListDegreeVersionsQuery>(q => q.DegreeCode == degreeCode), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result<DegreeVersionListResponse>.Success(expectedResponse));

            // Act
            var result = await _controller.ListDegreeVersions(degreeCode, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<DegreeVersionListResponse>(okResult.Value);
            Assert.Equal(degreeCode, response.DegreeCode);
            Assert.Equal(2, response.CurrentVersion);
            Assert.Equal(2, response.Versions.Count);
        }
    }
}
