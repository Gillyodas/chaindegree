using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.Core.Application.Degrees.Queries.GetDegrees;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class GetDegreesQueryHandlerTests
    {
        private readonly Mock<IDegreeQueryService> _degreeQueryServiceMock;
        private readonly Mock<ICurrentUserAccessor> _currentUserAccessorMock;
        private readonly GetDegreesQueryHandler _handler;

        public GetDegreesQueryHandlerTests()
        {
            _degreeQueryServiceMock = new Mock<IDegreeQueryService>();
            _currentUserAccessorMock = new Mock<ICurrentUserAccessor>();
            _handler = new GetDegreesQueryHandler(_degreeQueryServiceMock.Object, _currentUserAccessorMock.Object);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task Handle_InvalidPageIndex_ReturnsValidationFailure(int invalidPageIndex)
        {
            // Arrange
            var query = new GetDegreesQuery(invalidPageIndex, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Pagination.InvalidPageIndex", result.Error.Code);
            _degreeQueryServiceMock.Verify(s => s.GetDegreesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        [InlineData(999)]
        public async Task Handle_InvalidPageSize_ReturnsValidationFailure(int invalidPageSize)
        {
            // Arrange
            var query = new GetDegreesQuery(1, invalidPageSize);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Pagination.InvalidPageSize", result.Error.Code);
            _degreeQueryServiceMock.Verify(s => s.GetDegreesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NullInstitutionId_ReturnsInstitutionMismatchError()
        {
            // Arrange
            _currentUserAccessorMock.Setup(a => a.InstitutionId).Returns((Guid?)null);
            var query = new GetDegreesQuery(1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InstitutionMismatch, result.Error);
            _degreeQueryServiceMock.Verify(s => s.GetDegreesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyInstitutionId_ReturnsInstitutionMismatchError()
        {
            // Arrange
            _currentUserAccessorMock.Setup(a => a.InstitutionId).Returns(Guid.Empty);
            var query = new GetDegreesQuery(1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.InstitutionMismatch, result.Error);
            _degreeQueryServiceMock.Verify(s => s.GetDegreesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsQueryServiceWithCurrentTenantAndReturnsSuccess()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            _currentUserAccessorMock.Setup(a => a.InstitutionId).Returns(institutionId);

            var expectedResult = new PagedResult<DegreeListDto>(
                new List<DegreeListDto>
                {
                    new DegreeListDto(Guid.NewGuid(), "DEG-2026-000001", Guid.NewGuid(), "Nguyen Van A", "SE", "Gioi", "Confirmed", DateTime.UtcNow, "0x123")
                },
                totalCount: 1,
                pageIndex: 1,
                pageSize: 20
            );

            _degreeQueryServiceMock
                .Setup(s => s.GetDegreesAsync(institutionId, 1, 20, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var query = new GetDegreesQuery(1, 20);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedResult.TotalCount, result.Value.TotalCount);
            Assert.Equal(expectedResult.Items.Count, result.Value.Items.Count);
            _degreeQueryServiceMock.Verify(s => s.GetDegreesAsync(institutionId, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
