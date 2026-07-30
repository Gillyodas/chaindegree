using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Domain.Jobs;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Jobs;

using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Recruitment.Commands.PostJob
{
    public class PostJobCommandHandler : IRequestHandler<PostJobCommand, Result<PostJobResponse>>
    {
        private readonly IJobRepository _jobRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<PostJobCommandHandler> _logger;

        public PostJobCommandHandler(
            IJobRepository jobRepository,
            ICurrentUserAccessor currentUserAccessor,
            IBehaviorLogService behaviorLogService,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider,
            ILogger<PostJobCommandHandler> logger)
        {
            _jobRepository = jobRepository;
            _currentUserAccessor = currentUserAccessor;
            _behaviorLogService = behaviorLogService;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<Result<PostJobResponse>> Handle(PostJobCommand request, CancellationToken ct)
        {
            if (!_currentUserAccessor.IsAuthenticated)
            {
                return Result<PostJobResponse>.Failure(JobErrors.EmptyIdentifiers);
            }

            var recruiterId = _currentUserAccessor.UserId;
            var utcNow = _timeProvider.GetUtcNow();

            var jobResult = Job.Create(
                request.CompanyId,
                recruiterId,
                request.PartnerUniversityId,
                request.Title,
                request.Description,
                request.SalaryMin,
                request.SalaryMax,
                request.ApplicationStartDate,
                request.ApplicationEndDate,
                utcNow
            );

            if (jobResult.IsFailure)
            {
                return Result<PostJobResponse>.Failure(jobResult.Error);
            }

            var job = jobResult.Value;

            if (request.DegreeFilters != null && request.DegreeFilters.Count > 0)
            {
                foreach (var filter in request.DegreeFilters)
                {
                    job.AddFilter(filter.DegreeType, filter.RequiredMajor, filter.MinimumClassification);
                }
            }

            await _jobRepository.AddAsync(job, ct);
            await _unitOfWork.CommitAsync(ct);

            // Safe log (no PII)
            _logger.LogInformation("Job posted successfully: JobId={JobId}, RecruiterId={RecruiterId}", job.Id, recruiterId);

            await _behaviorLogService.LogAsync(
                ActionTypeEnum.POST_JOB,
                "Jobs",
                job.Id,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { job.Id, job.CompanyId, recruiterId, job.Title }),
                ct
            );

            return Result<PostJobResponse>.Success(new PostJobResponse(job.Id, job.Status.ToString(), job.CreatedAt));
        }
    }
}
