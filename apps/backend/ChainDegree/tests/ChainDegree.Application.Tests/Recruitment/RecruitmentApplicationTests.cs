using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Recruitment.Commands.ApplyForJob;
using ChainDegree.Core.Application.Recruitment.Commands.PostJob;
using ChainDegree.Core.Application.Recruitment.Options;
using ChainDegree.Core.Application.Recruitment.Queries.GetJobs;
using ChainDegree.Core.Application.Recruitment.Services;
using ChainDegree.Core.Domain.Applications;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.Jobs.Enums;
using ChainDegree.SharedKernel.DomainErrors.Applications;
using ChainDegree.SharedKernel.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ChainDegree.Application.Tests.Recruitment
{
    public class RecruitmentApplicationTests
    {
        private readonly Mock<IJobRepository> _mockJobRepo = new();
        private readonly Mock<IApplicationRepository> _mockAppRepo = new();
        private readonly Mock<IDegreeRepository> _mockDegreeRepo = new();
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor = new();
        private readonly Mock<IBehaviorLogService> _mockBehaviorLog = new();
        private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
        private readonly Mock<IReputationReadService> _mockReputationRead = new();
        private readonly Mock<ILogger<PostJobCommandHandler>> _mockPostLogger = new();
        private readonly Mock<ILogger<ApplyForJobCommandHandler>> _mockApplyLogger = new();
        private readonly TimeProvider _timeProvider = TimeProvider.System;

        public RecruitmentApplicationTests()
        {
            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);
        }

        [Fact]
        public async Task ApplyForJob_WithDifferentStudentDegree_ReturnsDegreeOwnershipMismatchError()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var otherStudentId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var degreeId = Guid.NewGuid();

            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);

            var now = DateTimeOffset.UtcNow;
            var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Title", "Desc", 1000, 2000, null, now.AddDays(5).UtcDateTime, now).Value;
            _mockJobRepo.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);

            var otherStudentDegree = CreateMockDegree(otherStudentId, "Computer Science", "Giỏi");
            _mockDegreeRepo.Setup(r => r.GetByIdAsync(degreeId, It.IsAny<CancellationToken>())).ReturnsAsync(otherStudentDegree);

            var handler = new ApplyForJobCommandHandler(
                _mockJobRepo.Object,
                _mockAppRepo.Object,
                _mockDegreeRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _timeProvider,
                _mockApplyLogger.Object
            );

            // Act
            var result = await handler.Handle(new ApplyForJobCommand(jobId, degreeId, false), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ApplicationErrors.DegreeOwnershipMismatch.Code, result.Error.Code);
        }

        [Fact]
        public async Task ApplyForJob_WhenUnderQualifiedWithoutForceSubmit_ReturnsFilterCriteriaNotSatisfiedError()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var degreeId = Guid.NewGuid();

            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);

            var now = DateTimeOffset.UtcNow;
            var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Title", "Desc", 1000, 2000, null, now.AddDays(5).UtcDateTime, now).Value;
            job.AddFilter(DegreeTypeEnum.Cu_Nhan, "Computer Science", "Xuất sắc");
            _mockJobRepo.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);

            var studentDegree = CreateMockDegree(studentId, "Computer Science", "Khá"); // Under qualified
            _mockDegreeRepo.Setup(r => r.GetByIdAsync(degreeId, It.IsAny<CancellationToken>())).ReturnsAsync(studentDegree);

            _mockAppRepo.Setup(r => r.ExistsAsync(studentId, jobId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var handler = new ApplyForJobCommandHandler(
                _mockJobRepo.Object,
                _mockAppRepo.Object,
                _mockDegreeRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _timeProvider,
                _mockApplyLogger.Object
            );

            // Act
            var result = await handler.Handle(new ApplyForJobCommand(jobId, degreeId, ForceSubmit: false), CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(ApplicationErrors.FilterCriteriaNotSatisfied.Code, result.Error.Code);
        }

        [Fact]
        public async Task ApplyForJob_WhenUnderQualifiedWithForceSubmit_SucceedsAsUnderQualified()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var degreeId = Guid.NewGuid();

            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);

            var now = DateTimeOffset.UtcNow;
            var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Title", "Desc", 1000, 2000, null, now.AddDays(5).UtcDateTime, now).Value;
            job.AddFilter(DegreeTypeEnum.Cu_Nhan, "Computer Science", "Xuất sắc");
            _mockJobRepo.Setup(r => r.GetByIdAsync(jobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);

            var studentDegree = CreateMockDegree(studentId, "Computer Science", "Khá");
            _mockDegreeRepo.Setup(r => r.GetByIdAsync(degreeId, It.IsAny<CancellationToken>())).ReturnsAsync(studentDegree);

            _mockAppRepo.Setup(r => r.ExistsAsync(studentId, jobId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var handler = new ApplyForJobCommandHandler(
                _mockJobRepo.Object,
                _mockAppRepo.Object,
                _mockDegreeRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _timeProvider,
                _mockApplyLogger.Object
            );

            // Act
            var result = await handler.Handle(new ApplyForJobCommand(jobId, degreeId, ForceSubmit: true), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Under_Qualified", result.Value.RankStatus);
            Assert.True(result.Value.IsForceSubmitted);
        }

        [Fact]
        public async Task GetJobsQuery_RanksJobsByCalculatedScoreDescending()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var partner1 = Guid.NewGuid();
            var partner2 = Guid.NewGuid();

            var job1 = Job.Create(Guid.NewGuid(), Guid.NewGuid(), partner1, "Job 1", "Desc", 1000, 2000, null, now.AddDays(5).UtcDateTime, now).Value;
            var job2 = Job.Create(Guid.NewGuid(), Guid.NewGuid(), partner2, "Job 2", "Desc", 5000, 10000, null, now.AddDays(5).UtcDateTime, now).Value;

            _mockJobRepo.Setup(r => r.GetActiveJobsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Job> { job1, job2 });

            _mockReputationRead.Setup(r => r.GetReputationScoresAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, int>
                {
                    { partner1, 400 },
                    { partner2, 1000 }
                });

            var options = Options.Create(new RankingOptions());
            var rankingService = new JobRankingService(options, _timeProvider);

            var queryHandler = new GetJobsQueryHandler(_mockJobRepo.Object, _mockReputationRead.Object, rankingService);

            // Act
            var result = await queryHandler.Handle(new GetJobsQuery(), CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Count);
            // Job 2 has higher salary and higher partner reputation -> Should be first
            Assert.Equal(job2.Id, result.Value[0].Id);
            Assert.Equal(job1.Id, result.Value[1].Id);
            Assert.True(result.Value[0].JobScore > result.Value[1].JobScore);
        }

        private static Degree CreateMockDegree(Guid studentId, string major, string classification)
        {
            var mockHashService = new Mock<IHashService>();
            mockHashService.Setup(h => h.GenerateSalt()).Returns(Result<string>.Success("a7d83bf92c81e3d0"));
            mockHashService.Setup(h => h.HashData(It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(Result<string>.Success("mocked_data_hash_local"));

            var plainJson = "{\"classification\":\"" + classification + "\",\"degreeCode\":\"DEG-2026-000001\",\"major\":\"" + major + "\"}";
            var cryptoData = CryptoSnapshot.Create(plainJson, mockHashService.Object).Value;

            return Degree.Create(
                1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                studentId,
                major,
                classification,
                cryptoData
            ).Value;
        }
    }
}
