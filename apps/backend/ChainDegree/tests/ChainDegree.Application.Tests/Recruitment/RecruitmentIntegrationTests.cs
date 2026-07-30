using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ChainDegree.Core.Domain.Applications.Application;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Recruitment.Commands.ApplyForJob;
using ChainDegree.Core.Application.Recruitment.Commands.PostJob;
using ChainDegree.Core.Application.Recruitment.Options;
using ChainDegree.Core.Application.Recruitment.Queries.GetJobs;
using ChainDegree.Core.Application.Recruitment.Services;
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
    public class RecruitmentIntegrationTests
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

        private readonly Dictionary<Guid, Job> _jobsDb = new();
        private readonly Dictionary<Guid, ApplicationEntity> _appsDb = new();
        private readonly Dictionary<Guid, Degree> _degreesDb = new();

        public RecruitmentIntegrationTests()
        {
            _mockUserAccessor.Setup(u => u.IsAuthenticated).Returns(true);

            // Set up in-memory simulation for repositories
            _mockJobRepo.Setup(r => r.AddAsync(It.IsAny<Job>(), It.IsAny<CancellationToken>()))
                .Callback<Job, CancellationToken>((j, ct) => _jobsDb[j.Id] = j)
                .Returns(Task.CompletedTask);

            _mockJobRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken ct) => _jobsDb.TryGetValue(id, out var j) ? j : null);

            _mockJobRepo.Setup(r => r.GetActiveJobsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string? search, CancellationToken ct) => new List<Job>(_jobsDb.Values));

            _mockAppRepo.Setup(r => r.AddAsync(It.IsAny<ApplicationEntity>(), It.IsAny<CancellationToken>()))
                .Callback<ApplicationEntity, CancellationToken>((a, ct) => _appsDb[a.Id] = a)
                .Returns(Task.CompletedTask);

            _mockAppRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid studentId, Guid jobId, CancellationToken ct) =>
                {
                    foreach (var app in _appsDb.Values)
                    {
                        if (app.StudentId == studentId && app.JobId == jobId) return true;
                    }
                    return false;
                });

            _mockDegreeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken ct) => _degreesDb.TryGetValue(id, out var d) ? d : null);
        }

        [Fact]
        public async Task FullRecruitmentWorkflow_PostJob_QueryJobs_ApplySuccessfully_VerifyInvariants()
        {
            // 1. Recruiter posts a job
            var recruiterId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var partnerUniversityId = Guid.NewGuid();
            _mockUserAccessor.Setup(u => u.UserId).Returns(recruiterId);

            var postJobHandler = new PostJobCommandHandler(
                _mockJobRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _timeProvider,
                _mockPostLogger.Object
            );

            var postJobCmd = new PostJobCommand(
                companyId,
                partnerUniversityId,
                "Backend Engineer",
                "Great job description",
                2000,
                4000,
                null,
                DateTime.UtcNow.AddDays(10),
                new List<DegreeFilterDto>
                {
                    new DegreeFilterDto(DegreeTypeEnum.Cu_Nhan, "Computer Science", "Giỏi")
                }
            );

            var postJobResult = await postJobHandler.Handle(postJobCmd, CancellationToken.None);
            Assert.True(postJobResult.IsSuccess);
            var jobId = postJobResult.Value.JobId;

            // 2. Query Jobs and verify ranking
            _mockReputationRead.Setup(r => r.GetReputationScoresAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, int> { { partnerUniversityId, 850 } });

            var rankingService = new JobRankingService(Options.Create(new RankingOptions()), _timeProvider);
            var getJobsHandler = new GetJobsQueryHandler(_mockJobRepo.Object, _mockReputationRead.Object, rankingService);

            var getJobsResult = await getJobsHandler.Handle(new GetJobsQuery(), CancellationToken.None);
            Assert.True(getJobsResult.IsSuccess);
            Assert.Single(getJobsResult.Value);
            Assert.Equal(jobId, getJobsResult.Value[0].Id);

            // 3. Student applies with valid matching degree
            var studentId = Guid.NewGuid();
            _mockUserAccessor.Setup(u => u.UserId).Returns(studentId);

            var studentDegree = CreateMockDegree(studentId, "Computer Science", "Xuất sắc");
            _degreesDb[studentDegree.Id] = studentDegree;

            var applyHandler = new ApplyForJobCommandHandler(
                _mockJobRepo.Object,
                _mockAppRepo.Object,
                _mockDegreeRepo.Object,
                _mockUserAccessor.Object,
                _mockBehaviorLog.Object,
                _mockUnitOfWork.Object,
                _timeProvider,
                _mockApplyLogger.Object
            );

            var applyCmd = new ApplyForJobCommand(jobId, studentDegree.Id, ForceSubmit: false);
            var applyResult = await applyHandler.Handle(applyCmd, CancellationToken.None);

            Assert.True(applyResult.IsSuccess);
            Assert.Equal("Highly_Qualified", applyResult.Value.RankStatus);
            Assert.Equal("Submitted", applyResult.Value.ProcessStatus);

            // 4. Duplicate apply check (Same student, same job -> Must be rejected)
            var duplicateResult = await applyHandler.Handle(applyCmd, CancellationToken.None);
            Assert.True(duplicateResult.IsFailure);
            Assert.Equal(ApplicationErrors.DuplicateApplication.Code, duplicateResult.Error.Code);

            // 5. IDOR check (User B tries to apply using User A's degree)
            var attackerStudentId = Guid.NewGuid();
            _mockUserAccessor.Setup(u => u.UserId).Returns(attackerStudentId);

            var idorApplyCmd = new ApplyForJobCommand(jobId, studentDegree.Id, ForceSubmit: false);
            var idorResult = await applyHandler.Handle(idorApplyCmd, CancellationToken.None);

            Assert.True(idorResult.IsFailure);
            Assert.Equal(ApplicationErrors.DegreeOwnershipMismatch.Code, idorResult.Error.Code);
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
