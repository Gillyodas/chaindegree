using System;
using ChainDegree.Core.Application.Recruitment.Options;
using ChainDegree.Core.Domain.Jobs;
using Microsoft.Extensions.Options;

namespace ChainDegree.Core.Application.Recruitment.Services
{
    public interface IJobRankingService
    {
        double CalculateJobScore(Job job, int reputationScore);
    }

    public class JobRankingService : IJobRankingService
    {
        private readonly RankingOptions _options;
        private readonly TimeProvider _timeProvider;

        public JobRankingService(IOptions<RankingOptions> options, TimeProvider timeProvider)
        {
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public double CalculateJobScore(Job job, int reputationScore)
        {
            if (job == null)
                return 0;

            double salaryAvg = (double)(job.SalaryMin + job.SalaryMax) / 2.0;
            if (salaryAvg <= 0) salaryAvg = 1.0;

            double salaryScore = _options.WeightSalary * Math.Log(salaryAvg);
            double repScore = _options.WeightReputation * ((double)reputationScore / 1000.0);

            double daysSinceCreated = Math.Max(0, (_timeProvider.GetUtcNow() - job.CreatedAt).TotalDays);
            double timeScore = _options.WeightTime / (1.0 + daysSinceCreated);

            return salaryScore + repScore + timeScore;
        }
    }
}
