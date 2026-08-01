using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationEntity = ChainDegree.Core.Domain.Applications.Application;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.Applications.Enums;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Applications;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Recruitment.Commands.ApplyForJob
{
    public class ApplyForJobCommandHandler : IRequestHandler<ApplyForJobCommand, Result<ApplyForJobResponse>>
    {
        private readonly IJobRepository _jobRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IDegreeRepository _degreeRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<ApplyForJobCommandHandler> _logger;

        public ApplyForJobCommandHandler(
            IJobRepository jobRepository,
            IApplicationRepository applicationRepository,
            IDegreeRepository degreeRepository,
            ICurrentUserAccessor currentUserAccessor,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider,
            ILogger<ApplyForJobCommandHandler> logger)
        {
            _jobRepository = jobRepository;
            _applicationRepository = applicationRepository;
            _degreeRepository = degreeRepository;
            _currentUserAccessor = currentUserAccessor;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Result<ApplyForJobResponse>> Handle(ApplyForJobCommand request, CancellationToken ct)
        {
            if (!_currentUserAccessor.IsAuthenticated)
            {
                return Result<ApplyForJobResponse>.Failure(ApplicationErrors.EmptyIdentifiers);
            }

            var studentId = _currentUserAccessor.UserId;
            var utcNow = _timeProvider.GetUtcNow();

            // 1. Fetch Job & check deadline / closed status
            var job = await _jobRepository.GetByIdAsync(request.JobId, ct);
            if (job == null || job.IsExpired(utcNow))
            {
                return Result<ApplyForJobResponse>.Failure(ApplicationErrors.JobClosedOrExpired);
            }

            // 2. Fetch Degree & verify ownership (IDOR check)
            var degree = await _degreeRepository.GetByIdAsync(request.DegreeId, ct);
            if (degree == null || degree.StudentId != studentId)
            {
                return Result<ApplyForJobResponse>.Failure(ApplicationErrors.DegreeOwnershipMismatch);
            }

            // 3. Verify Revoked status rule
            if (degree.Status == StatusEnum.Revoked || degree.Status == StatusEnum.Pending_Revocation)
            {
                return Result<ApplyForJobResponse>.Failure(ApplicationErrors.RevokedDegreeCannotBeSubmitted);
            }

            // 4. Duplicate application check
            bool alreadyApplied = await _applicationRepository.ExistsAsync(studentId, request.JobId, ct);
            if (alreadyApplied)
            {
                return Result<ApplyForJobResponse>.Failure(ApplicationErrors.DuplicateApplication);
            }

            // 5. Matching evaluation (Server-computed, not client-trusted)
            var evaluatedRankStatus = job.EvaluateApplication(degree);

            if (evaluatedRankStatus == ApplicationRankStatusEnum.Under_Qualified && !request.ForceSubmit)
            {
                return Result<ApplyForJobResponse>.Failure(ApplicationErrors.FilterCriteriaNotSatisfied);
            }

            bool isForceSubmitted = evaluatedRankStatus == ApplicationRankStatusEnum.Under_Qualified && request.ForceSubmit;

            var applicationResult = ApplicationEntity.Create(
                request.JobId,
                studentId,
                request.DegreeId,
                evaluatedRankStatus,
                isForceSubmitted,
                utcNow
            );

            if (applicationResult.IsFailure)
            {
                return Result<ApplyForJobResponse>.Failure(applicationResult.Error);
            }

            var application = applicationResult.Value;

            await _applicationRepository.AddAsync(application, ct);
            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation("Job application submitted: ApplicationId={ApplicationId}, JobId={JobId}, StudentId={StudentId}, DegreeId={DegreeId}, RankStatus={RankStatus}",
                application.Id, request.JobId, studentId, request.DegreeId, application.RankStatus);

            await _behaviorLogService.LogAsync(
                ActionTypeEnum.APPLY_JOB,
                "Applications",
                application.Id,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { application.Id, application.JobId, studentId, request.DegreeId, application.RankStatus }),
                ct
            );

            return Result<ApplyForJobResponse>.Success(new ApplyForJobResponse(
                application.Id,
                application.JobId,
                application.StudentId,
                application.RankStatus.ToString(),
                application.ProcessStatus.ToString(),
                application.IsForceSubmitted
            ));
        }
    }
}
