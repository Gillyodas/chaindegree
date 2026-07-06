using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.Core.Application.Degrees.Queries.GetBatchStatus;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class GetBatchStatusQueryHandlerTests
    {
        private readonly Mock<IBatchQueryService> _mockQueryService;
        private readonly GetBatchStatusQueryHandler _handler;

        public GetBatchStatusQueryHandlerTests()
        {
            _mockQueryService = new Mock<IBatchQueryService>();
            _handler = new GetBatchStatusQueryHandler(_mockQueryService.Object);
        }

        [Fact]
        public async Task Handle_WithExistingBatch_ReturnsSuccess()
        {
            // Arrange
            var batchId = Guid.NewGuid();
            var response = new BatchQueryResponse(
                batchId,
                "BATCH_TEST",
                "Pending",
                10,
                null,
                null,
                null,
                180,
                null,
                DateTime.UtcNow,
                null
            );

            _mockQueryService.Setup(q => q.GetBatchStatusAsync(batchId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(response);

            var query = new GetBatchStatusQuery(batchId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(batchId, result.Value.BatchId);
            Assert.Equal("BATCH_TEST", result.Value.BatchName);
        }

        [Fact]
        public async Task Handle_WithNonExistentBatch_ReturnsFailure()
        {
            // Arrange
            var batchId = Guid.NewGuid();
            _mockQueryService.Setup(q => q.GetBatchStatusAsync(batchId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync((BatchQueryResponse?)null);

            var query = new GetBatchStatusQuery(batchId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.BatchNotFound, result.Error);
        }
    }
}
