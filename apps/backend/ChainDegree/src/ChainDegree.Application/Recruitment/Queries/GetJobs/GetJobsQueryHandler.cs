using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Application.Abstractions.Services;
using ChainDegree.Core.Application.Recruitment.Services;
using ChainDegree.SharedKernel.Result;
using MediatR;

namespace ChainDegree.Core.Application.Recruitment.Queries.GetJobs
{
    public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, Result<IReadOnlyList<JobResponse>>>
    {
        private readonly IJobRepository _jobRepository;
        private readonly IReputationReadService _reputationReadService;
        private readonly IJobRankingService _jobRankingService;

        public GetJobsQueryHandler(
            IJobRepository jobRepository,
            IReputationReadService reputationReadService,
            IJobRankingService jobRankingService)
        {
            _jobRepository = jobRepository;
            _reputationReadService = reputationReadService;
            _jobRankingService = jobRankingService;
        }

        public async Task<Result<IReadOnlyList<JobResponse>>> Handle(GetJobsQuery request, CancellationToken ct)
        {
            var jobs = await _jobRepository.GetActiveJobsAsync(request.SearchTerm, ct);
            if (jobs.Count == 0)
            {
                return Result<IReadOnlyList<JobResponse>>.Success(Array.Empty<JobResponse>());
            }

            // Batch load reputation scores for all partner universities in the list (prevents N+1 queries)
            var partnerIds = jobs
                .Where(j => j.PartnerUniversityId.HasValue)
                .Select(j => j.PartnerUniversityId!.Value)
                .Distinct()
                .ToList();

            var reputationMap = await _reputationReadService.GetReputationScoresAsync(partnerIds, ct);

            var jobResponses = jobs.Select(job =>
            {
                int repScore = 500; // Default floor reputation score
                if (job.PartnerUniversityId.HasValue && reputationMap.TryGetValue(job.PartnerUniversityId.Value, out var score))
                {
                    repScore = score;
                }

                double jobScore = _jobRankingService.CalculateJobScore(job, repScore);

                return new JobResponse(
                    job.Id,
                    job.CompanyId,
                    job.PartnerUniversityId,
                    job.Title,
                    job.Description,
                    job.SalaryMin,
                    job.SalaryMax,
                    job.ApplicationStartDate,
                    job.ApplicationEndDate,
                    job.Status.ToString(),
                    jobScore,
                    job.CreatedAt
                );
            })
            .OrderByDescending(j => j.JobScore)
            .ToList();

            return Result<IReadOnlyList<JobResponse>>.Success(jobResponses);
        }
    }
}
