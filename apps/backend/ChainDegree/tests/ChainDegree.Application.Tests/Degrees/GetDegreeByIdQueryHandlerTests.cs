using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Queries;
using ChainDegree.Core.Application.Degrees.Queries.DTOs;
using ChainDegree.Core.Application.Degrees.Queries.GetDegreeById;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class GetDegreeByIdQueryHandlerTests
    {
        private readonly Mock<IDegreeQueryService> _degreeQueryServiceMock;
        private readonly Mock<ICurrentUserAccessor> _currentUserAccessorMock;
        private readonly GetDegreeByIdQueryHandler _handler;

        public GetDegreeByIdQueryHandlerTests()
        {
            _degreeQueryServiceMock = new Mock<IDegreeQueryService>();
            _currentUserAccessorMock = new Mock<ICurrentUserAccessor>();
            _handler = new GetDegreeByIdQueryHandler(_degreeQueryServiceMock.Object, _currentUserAccessorMock.Object);
        }

        [Fact]
        public async Task Handle_EmptyDegreeId_ReturnsNotFound()
        {
            // Arrange
            var query = new GetDegreeByIdQuery(Guid.Empty);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.NotFound, result.Error);
            _degreeQueryServiceMock.Verify(s => s.GetDegreeByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NullInstitutionId_ReturnsNotFoundToPreventEnumeration()
        {
            // Arrange
            _currentUserAccessorMock.Setup(a => a.InstitutionId).Returns((Guid?)null);
            var query = new GetDegreeByIdQuery(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.NotFound, result.Error);
            _degreeQueryServiceMock.Verify(s => s.GetDegreeByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DegreeNotFoundOrCrossTenant_ReturnsNotFound()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var degreeId = Guid.NewGuid();
            _currentUserAccessorMock.Setup(a => a.InstitutionId).Returns(institutionId);

            _degreeQueryServiceMock
                .Setup(s => s.GetDegreeByIdAsync(degreeId, institutionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((DegreeDetailDto?)null);

            var query = new GetDegreeByIdQuery(degreeId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(DegreeErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsDegreeDetailDto()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var degreeId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();

            _currentUserAccessorMock.Setup(a => a.InstitutionId).Returns(institutionId);

            var expectedDetail = new DegreeDetailDto(
                degreeId,
                "DEG-2026-000099",
                institutionId,
                registrarId,
                studentId,
                "Tran Van B",
                "Computer Science",
                "Xuat Sac",
                "Confirmed",
                DateTime.UtcNow,
                "0xabc",
                1,
                DateTime.UtcNow,
                null
            );

            _degreeQueryServiceMock
                .Setup(s => s.GetDegreeByIdAsync(degreeId, institutionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDetail);

            var query = new GetDegreeByIdQuery(degreeId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedDetail.Id, result.Value.Id);
            Assert.Equal(expectedDetail.DegreeCode, result.Value.DegreeCode);
            Assert.Equal(expectedDetail.StudentFullName, result.Value.StudentFullName);
            _degreeQueryServiceMock.Verify(s => s.GetDegreeByIdAsync(degreeId, institutionId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
