using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Infrastructure.Services
{
    public class ReputationReadService : IReputationReadService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReputationReadService> _logger;

        public ReputationReadService(IServiceProvider serviceProvider, ILogger<ReputationReadService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<Dictionary<Guid, int>> GetReputationScoresAsync(IEnumerable<Guid> partnerUniversityIds, CancellationToken ct = default)
        {
            var result = new Dictionary<Guid, int>();
            var idsList = partnerUniversityIds.Distinct().ToList();
            if (idsList.Count == 0) return result;

            try
            {
                // Try resolving ReputationRepository dynamically if module is loaded
                var repRepoType = Type.GetType("ChainDegree.Reputation.Domain.Repositories.IReputationRepository, ChainDegree.Reputation");
                if (repRepoType != null)
                {
                    var repRepo = _serviceProvider.GetService(repRepoType);
                    if (repRepo != null)
                    {
                        var method = repRepoType.GetMethod("GetByInstitutionIdAsync");
                        if (method != null)
                        {
                            foreach (var id in idsList)
                            {
                                var task = method.Invoke(repRepo, new object[] { id, ct }) as Task;
                                if (task != null)
                                {
                                    await task.ConfigureAwait(false);
                                    var property = task.GetType().GetProperty("Result");
                                    var repScoreObj = property?.GetValue(task);
                                    if (repScoreObj != null)
                                    {
                                        var scoreProp = repScoreObj.GetType().GetProperty("CurrentScore");
                                        if (scoreProp != null && scoreProp.GetValue(repScoreObj) is int score)
                                        {
                                            result[id] = score;
                                            continue;
                                        }
                                    }
                                }
                                result[id] = 500;
                            }
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch reputation from Reputation module. Using fallback score 500.");
            }

            // Fallback: 500 default score
            foreach (var id in idsList)
            {
                result[id] = 500;
            }

            return result;
        }

        public async Task<int> GetReputationScoreAsync(Guid? partnerUniversityId, CancellationToken ct = default)
        {
            if (!partnerUniversityId.HasValue) return 500;
            var dict = await GetReputationScoresAsync(new[] { partnerUniversityId.Value }, ct);
            return dict.TryGetValue(partnerUniversityId.Value, out var score) ? score : 500;
        }
    }
}
