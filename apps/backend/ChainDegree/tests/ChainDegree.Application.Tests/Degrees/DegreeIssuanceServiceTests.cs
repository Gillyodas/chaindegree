using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Policies;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Services;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.Result;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Degrees
{
    public class DegreeIssuanceServiceTests
    {
        private readonly Mock<IDegreeRepository> _mockRepo;
        private readonly Mock<IDegreeDuplicatePolicy> _mockDuplicatePolicy;
        private readonly Mock<IDegreeHashService> _mockHashService;
        private readonly Mock<ILogger<DegreeIssuanceService>> _mockLogger;
        private readonly DegreeIssuanceService _service;

        public DegreeIssuanceServiceTests()
        {
            _mockRepo = new Mock<IDegreeRepository>();
            _mockDuplicatePolicy = new Mock<IDegreeDuplicatePolicy>();
            _mockHashService = new Mock<IDegreeHashService>();
            _mockLogger = new Mock<ILogger<DegreeIssuanceService>>();

            _service = new DegreeIssuanceService(
                _mockRepo.Object,
                _mockDuplicatePolicy.Object,
                _mockHashService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task IssueDegreesAsync_WithValidItems_ReturnsPartialResultWithSuccesses()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var items = new List<IssueDegreeItemDto>
            {
                new(studentId, "Software Engineering", "Giỏi", DateTime.UtcNow)
            };

            var mockDomainHash = new Mock<IHashService>();
            mockDomainHash.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("mocked_salt_123456"));
            mockDomainHash.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>())).Returns(Result<string>.Success("mocked_hash_value"));
            var fakeCryptoSnapshot = CryptoSnapshot.Create("canonical_json", mockDomainHash.Object).Value;

            _mockRepo.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockDuplicatePolicy.Setup(d => d.IsDuplicateAsync(institutionId, studentId, "Software Engineering", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
            
            _mockHashService.Setup(h => h.RecalculateAsync(It.IsAny<DegreeData>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(fakeCryptoSnapshot);

            // Act
            var result = await _service.IssueDegreesAsync(institutionId, registrarId, items, CancellationToken.None);

            // Assert
            Assert.Empty(result.Failures);
            Assert.Single(result.Successes);
            var degree = result.Successes[0];
            Assert.Equal(studentId, degree.StudentId);
            Assert.Equal("Software Engineering", degree.Major);
            Assert.Equal("Giỏi", degree.Classification);
        }

        [Fact]
        public async Task IssueDegreesAsync_WithDuplicateItem_AddsToFailures()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var items = new List<IssueDegreeItemDto>
            {
                new(studentId, "Software Engineering", "Giỏi", DateTime.UtcNow)
            };

            _mockRepo.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockDuplicatePolicy.Setup(d => d.IsDuplicateAsync(institutionId, studentId, "Software Engineering", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(true);

            // Act
            var result = await _service.IssueDegreesAsync(institutionId, registrarId, items, CancellationToken.None);

            // Assert
            Assert.Empty(result.Successes);
            Assert.Single(result.Failures);
            Assert.Equal(studentId, result.Failures[0].StudentId);
            Assert.Contains("already exists", result.Failures[0].Reason);
        }

        [Fact]
        public async Task IssueDegreesAsync_WithCanonicalizationFailure_AddsToFailures()
        {
            // Arrange
            var institutionId = Guid.NewGuid();
            var registrarId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var items = new List<IssueDegreeItemDto>
            {
                new(studentId, "Software Engineering", "Giỏi", DateTime.UtcNow)
            };

            _mockRepo.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockDuplicatePolicy.Setup(d => d.IsDuplicateAsync(institutionId, studentId, "Software Engineering", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);

            _mockHashService.Setup(h => h.RecalculateAsync(It.IsAny<DegreeData>(), It.IsAny<CancellationToken>()))
                            .ThrowsAsync(new InvalidOperationException("Canonicalization failed"));

            // Act
            var result = await _service.IssueDegreesAsync(institutionId, registrarId, items, CancellationToken.None);

            // Assert
            Assert.Empty(result.Successes);
            Assert.Single(result.Failures);
            Assert.Equal(studentId, result.Failures[0].StudentId);
            Assert.Equal("Canonicalization failed", result.Failures[0].Reason);
        }
    }
}
