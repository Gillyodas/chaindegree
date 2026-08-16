using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Degrees.Queries.ListDegreeVersions;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class ListDegreeVersionsQueryHandlerTests
    {
        private readonly Mock<IDegreeRepository> _degreeRepositoryMock;
        private readonly Mock<ILogger<ListDegreeVersionsQueryHandler>> _loggerMock;
        private readonly ListDegreeVersionsQueryHandler _handler;

        public ListDegreeVersionsQueryHandlerTests()
        {
            _degreeRepositoryMock = new Mock<IDegreeRepository>();
            _loggerMock = new Mock<ILogger<ListDegreeVersionsQueryHandler>>();
            _handler = new ListDegreeVersionsQueryHandler(_degreeRepositoryMock.Object, _loggerMock.Object);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Handle_EmptyOrWhitespaceDegreeCode_ReturnsNotFound(string? degreeCode)
        {
            // Arrange
            var query = new ListDegreeVersionsQuery(degreeCode!);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.NotFound, result.Error);
            _degreeRepositoryMock.Verify(r => r.GetDegreeVersionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DegreeNotFound_ReturnsNotFound()
        {
            // Arrange
            var degreeCode = "DEG-2026-999999";
            _degreeRepositoryMock
                .Setup(r => r.GetDegreeVersionsAsync(degreeCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DegreeVersionListResponse?)null);

            var query = new ListDegreeVersionsQuery(degreeCode);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.NotFound, result.Error);
            _degreeRepositoryMock.Verify(r => r.GetDegreeVersionsAsync(degreeCode, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidDegreeCode_ReturnsSuccessWithVersions()
        {
            // Arrange
            var degreeCode = "DEG-2026-000001";
            var expectedResponse = new DegreeVersionListResponse(
                degreeCode,
                3,
                new List<DegreeVersionItem>
                {
                    new DegreeVersionItem(3, DateTime.UtcNow, true),
                    new DegreeVersionItem(2, DateTime.UtcNow.AddMonths(-1), false),
                    new DegreeVersionItem(1, DateTime.UtcNow.AddMonths(-2), false)
                });

            _degreeRepositoryMock
                .Setup(r => r.GetDegreeVersionsAsync(degreeCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var query = new ListDegreeVersionsQuery(degreeCode);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal(degreeCode, result.Value.DegreeCode);
            Assert.Equal(3, result.Value.CurrentVersion);
            Assert.Equal(3, result.Value.Versions.Count);
            Assert.True(result.Value.Versions[0].IsCurrent);
            Assert.Equal(3, result.Value.Versions[0].Version);
            Assert.False(result.Value.Versions[1].IsCurrent);
            Assert.Equal(2, result.Value.Versions[1].Version);
            Assert.False(result.Value.Versions[2].IsCurrent);
            Assert.Equal(1, result.Value.Versions[2].Version);
        }
    }
}
