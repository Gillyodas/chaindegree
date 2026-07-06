using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Policies;
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
        private readonly Mock<IJsonCanonicalizer> _mockCanonicalizer;
        private readonly Mock<IHashService> _mockHashService;
        private readonly Mock<ILogger<DegreeIssuanceService>> _mockLogger;
        private readonly DegreeIssuanceService _service;

        public DegreeIssuanceServiceTests()
        {
            _mockRepo = new Mock<IDegreeRepository>();
            _mockDuplicatePolicy = new Mock<IDegreeDuplicatePolicy>();
            _mockCanonicalizer = new Mock<IJsonCanonicalizer>();
            _mockHashService = new Mock<IHashService>();
            _mockLogger = new Mock<ILogger<DegreeIssuanceService>>();

            _service = new DegreeIssuanceService(
                _mockRepo.Object,
                _mockDuplicatePolicy.Object,
                _mockCanonicalizer.Object,
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

            _mockRepo.Setup(r => r.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0L);
            _mockDuplicatePolicy.Setup(d => d.IsDuplicateAsync(institutionId, studentId, "Software Engineering", It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(false);
            
            _mockCanonicalizer.Setup(c => c.Canonicalize(It.IsAny<object>()))
                              .Returns(Result<string>.Success("canonical_json"));

            _mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("mocked_salt_123456"));
            _mockHashService.Setup(h => h.HashData("canonical_json", "mocked_salt_123456"))
                            .Returns(Result<string>.Success("mocked_hash_value"));

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

            _mockCanonicalizer.Setup(c => c.Canonicalize(It.IsAny<object>()))
                              .Returns(Result<string>.Failure(CryptoErrors.CanonicalizationFailed));

            // Act
            var result = await _service.IssueDegreesAsync(institutionId, registrarId, items, CancellationToken.None);

            // Assert
            Assert.Empty(result.Successes);
            Assert.Single(result.Failures);
            Assert.Equal(studentId, result.Failures[0].StudentId);
            Assert.Equal(CryptoErrors.CanonicalizationFailed.Message, result.Failures[0].Reason);
        }
    }
}
