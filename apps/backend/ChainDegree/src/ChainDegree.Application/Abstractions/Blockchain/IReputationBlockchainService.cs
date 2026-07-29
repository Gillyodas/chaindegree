using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Blockchain;

public interface IReputationBlockchainService
{
    Task<string> AnchorHistoryHashAsync(Guid historyId, string historyHash, CancellationToken ct = default);
}
