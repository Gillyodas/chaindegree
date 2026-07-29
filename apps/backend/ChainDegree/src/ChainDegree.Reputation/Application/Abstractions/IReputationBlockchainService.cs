using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Reputation.Application.Abstractions;

public interface IReputationBlockchainService
{
    Task<string> AnchorHistoryHashAsync(Guid historyId, string historyHash, CancellationToken ct = default);
}
