using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Reputation.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Reputation.Infrastructure.Blockchain;

public class NethereumReputationBlockchainService : IReputationBlockchainService
{
    private readonly ILogger<NethereumReputationBlockchainService> _logger;

    public NethereumReputationBlockchainService(ILogger<NethereumReputationBlockchainService> logger)
    {
        _logger = logger;
    }

    public async Task<string> AnchorHistoryHashAsync(Guid historyId, string historyHash, CancellationToken ct = default)
    {
        _logger.LogInformation("Anchoring reputation history hash {HistoryHash} for record {HistoryId} to Besu network...", historyHash, historyId);

        // Simulate async network latency to Besu node
        await Task.Delay(50, ct);

        using var sha256 = SHA256.Create();
        var txHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"tx:{historyId}:{historyHash}:{DateTime.UtcNow.Ticks}"));
        var mockTxHash = "0x" + Convert.ToHexStringLower(txHashBytes);

        _logger.LogInformation("Successfully anchored reputation history {HistoryId} with TxHash: {TxHash}", historyId, mockTxHash);
        return mockTxHash;
    }
}
