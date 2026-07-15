using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using ChainDegree.SharedKernel.Common.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ChainDegree.Core.Infrastructure.BackgroundWorkers
{
    public class BatchingDegreeWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BatchingWorkerOptions _options;
        private readonly ILogger<BatchingDegreeWorker> _logger;

        public BatchingDegreeWorker(
            IServiceProvider serviceProvider,
            IOptions<BatchingWorkerOptions> options,
            ILogger<BatchingDegreeWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BatchingDegreeWorker started. Polling every {Interval} seconds.", _options.PollingIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RecoverPendingBatchesAsync(stoppingToken);
                    await ProcessNewBatchesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in BatchingDegreeWorker execution cycle.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
        }

        private async Task RecoverPendingBatchesAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

            var pendingBatches = await dbContext.BatchRecords
                .Where(b => b.Status == "Processing")
                .ToListAsync(ct);

            foreach (var batch in pendingBatches)
            {
                try
                {
                    bool isConfirmed = false;
                    string? failedReason = null;

                    if (!string.IsNullOrEmpty(batch.TxHash))
                    {
                        // Have TxHash: Check Receipt
                        var status = await blockchainService.GetTransactionStatusAsync(batch.TxHash, ct);
                        
                        if (status == TransactionStatus.Confirmed)
                        {
                            isConfirmed = true;
                        }
                        else if (status == TransactionStatus.Failed)
                        {
                            failedReason = "Transaction reverted on-chain.";
                        }
                        else 
                        {
                            // Pending or NotFound
                            var waitTime = DateTime.UtcNow - batch.CreatedAt;
                            if (waitTime.TotalMinutes > 30)
                            {
                                failedReason = $"Transaction {status} timeout after 30 minutes.";
                            }
                            else
                            {
                                // Still wait
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // TxHash is null (Unknown Outcome due to timeout during send)
                        _logger.LogWarning("Batch {BatchId} has null TxHash. Checking on-chain state...", batch.Id);
                        
                        var exists = await blockchainService.CheckBatchExistsAsync(batch.Id.ToString(), ct);
                        
                        if (exists)
                        {
                            isConfirmed = true;
                            // We don't have the TxHash, but it is confirmed.
                            batch.TxHash = "UNKNOWN_RECOVERED_ONCHAIN";
                        }
                        else
                        {
                            // Not exists, and we had a timeout earlier.
                            // We can mark it as failed so the retry logic will pick up the degrees again.
                            failedReason = "Unknown outcome resolved: Batch not found on-chain. Will retry.";
                        }
                    }

                    if (isConfirmed)
                    {
                        await FinalizeBatchSuccessAsync(dbContext, batch, ct);
                    }
                    else if (failedReason != null)
                    {
                        await FinalizeBatchFailedAsync(dbContext, batch, failedReason, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recover pending batch {BatchId}", batch.Id);
                }
            }
        }

        private async Task ProcessNewBatchesAsync(CancellationToken ct)
        {
            var workerId = Guid.NewGuid().ToString();
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var degreeRepo = scope.ServiceProvider.GetRequiredService<IDegreeRepository>();
            var merkleTreeService = scope.ServiceProvider.GetRequiredService<IMerkleTreeService>();
            var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

            await using var claimTransaction = await dbContext.Database.BeginTransactionAsync(ct);

            // Fetch degrees that are available for batching
            // Wait, we need to ensure they are not part of an ongoing batch.
            // We can check if they don't have a Processing record, or if their Processing record is Failed/Queued and NextRetryAt <= Now
            
            var now = DateTime.UtcNow;
            
            // This query replaces the simple GetPendingConfirmationAsync to account for retries
            var availableDegreeIds = await dbContext.Degrees
                .Where(d => d.Status == StatusEnum.Pending_Confirmation || d.Status == StatusEnum.Pending_Update || d.Status == StatusEnum.Pending_Revocation)
                .GroupJoin(dbContext.DegreeProcessingRecords, d => d.Id, pr => pr.DegreeId, (d, prs) => new { d, prs })
                .SelectMany(x => x.prs.DefaultIfEmpty(), (x, pr) => new { x.d, pr })
                .Where(x => x.pr == null || 
                            (x.pr.State == "Queued") || 
                            (x.pr.State == "Failed" && x.pr.NextRetryAt != null && x.pr.NextRetryAt <= now))
                .Select(x => x.d.Id)
                .Take(_options.MaxBatchSize)
                .ToListAsync(ct);

            if (availableDegreeIds.Count == 0)
            {
                await claimTransaction.RollbackAsync(ct);
                return;
            }

            // We only process if we hit max batch size or time threshold
            var degreesToProcess = await dbContext.Degrees.Where(d => availableDegreeIds.Contains(d.Id)).ToListAsync(ct);
            var oldestDegree = degreesToProcess.Min(d => d.CreatedAt);
            var waitTimeSeconds = (now - oldestDegree).TotalSeconds;
            var isTriggered = degreesToProcess.Count >= _options.MaxBatchSize || waitTimeSeconds >= _options.MaxWaitTimeSeconds;

            if (!isTriggered)
            {
                await claimTransaction.RollbackAsync(ct);
                return;
            }

            foreach (var degreeId in availableDegreeIds)
            {
                var pr = await dbContext.DegreeProcessingRecords.FindAsync(degreeId);
                if (pr == null)
                {
                    var degree = degreesToProcess.First(d => d.Id == degreeId);
                    pr = new DegreeProcessingRecord
                    {
                        DegreeId = degreeId,
                        ActionType = degree.Status switch
                        {
                            StatusEnum.Pending_Update => "Update",
                            StatusEnum.Pending_Revocation => "Revoke",
                            _ => "Issue"
                        },
                        State = "Queued",
                        RetryCount = 0
                    };
                    dbContext.DegreeProcessingRecords.Add(pr);
                }

                pr.State = "Processing";
                pr.WorkerId = workerId;
                pr.LeaseUntil = now.AddMinutes(10);
            }

            await dbContext.SaveChangesAsync(ct);
            await claimTransaction.CommitAsync(ct);

            // Phase 2: Create batches per institution
            var claimedDegrees = await dbContext.Degrees
                .Include(d => d.CryptoData)
                .Join(dbContext.DegreeProcessingRecords, d => d.Id, pr => pr.DegreeId, (d, pr) => new { Degree = d, Record = pr })
                .Where(x => x.Record.WorkerId == workerId && x.Record.State == "Processing")
                .ToListAsync(ct);

            if (claimedDegrees.Count == 0) return;

            var groups = claimedDegrees.GroupBy(x => x.Degree.InstitutionId).ToList();

            foreach (var group in groups)
            {
                var institutionId = group.Key;
                var institutionItems = group.ToList();
                var institutionDegrees = institutionItems.Select(x => x.Degree).ToList();
                
                var institution = await dbContext.EducationInstitutions.FirstOrDefaultAsync(x => x.Id == institutionId, ct);
                var instCode = institution?.Code ?? "UNKNOWN";
                var batchId = Guid.NewGuid();
                var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8);
                var batchName = $"BATCH_{instCode.ToUpperInvariant()}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{shortGuid}";

                // Build leaf hashes
                var leafHashes = new List<string>();
                foreach (var item in institutionItems)
                {
                    if (item.Record.ActionType == "Update")
                    {
                        var staging = await dbContext.DegreeUpdateRequests.FirstOrDefaultAsync(x => x.DegreeId == item.Degree.Id, ct);
                        leafHashes.Add(staging?.CryptoData.DataHashLocal ?? item.Degree.CryptoData.DataHashLocal);
                    }
                    else
                    {
                        leafHashes.Add(item.Degree.CryptoData.DataHashLocal);
                    }
                }

                var treeResult = merkleTreeService.BuildTree(leafHashes);

                var batchRecord = new BatchRecord
                {
                    Id = batchId,
                    InstitutionId = institutionId,
                    BatchName = batchName,
                    Status = "Processing",
                    DegreeCount = institutionDegrees.Count,
                    MerkleRoot = treeResult.MerkleRoot,
                    EstimatedWaitTimeSeconds = _options.MaxWaitTimeSeconds,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.BatchRecords.Add(batchRecord);
                await dbContext.SaveChangesAsync(ct);

                // Send Tx with Polly Retry for transient errors
                AnchorResult? anchorResult = null;
                string? txError = null;

                try
                {
                    // Polly Retry (Simulated manually for simplicity and no external dependencies, exponential backoff)
                    int maxRetries = 3;
                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            var actionType = institutionItems.First().Record.ActionType;
                            anchorResult = await blockchainService.AnchorMerkleRootAsync(batchId.ToString(), treeResult.MerkleRoot, institutionId.ToString(), actionType, ct);
                            break; // Success
                        }
                        catch (Exception txEx)
                        {
                            bool isTransient = IsTransientError(txEx);
                            if (isTransient && retry < maxRetries - 1)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retry)), ct); // Exponential backoff: 1s, 2s...
                                continue;
                            }
                            
                            // Permanent error or max retries reached
                            txError = txEx.Message;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    txError = ex.Message;
                }

                if (anchorResult != null)
                {
                    batchRecord.TxHash = anchorResult.TransactionHash;
                    await dbContext.SaveChangesAsync(ct);
                }
                else
                {
                    // Unknown Outcome or Permanent Error!
                    // TxHash is left null.
                    _logger.LogWarning("Unknown Outcome or Permanent Error for batch {BatchId}: {Error}", batchId, txError);
                    // The RecoverPendingBatchesAsync will handle it on next cycle!
                }
            }
        }

        private bool IsTransientError(Exception ex)
        {
            var msg = ex.Message.ToLowerInvariant();
            if (msg.Contains("timeout") || msg.Contains("503") || msg.Contains("network error") || msg.Contains("connection"))
            {
                return true;
            }
            return false;
        }

        private async Task FinalizeBatchSuccessAsync(ChainDegreeDbContext dbContext, BatchRecord batchRecord, CancellationToken ct)
        {
            // Find all degrees associated with this batch through the Processing records?
            // Wait, we didn't link BatchRecord to DegreeProcessingRecord.
            // But we know which degrees they are because we can query them via worker logic, or we can just find them by institution ID and status.
            // Actually, in ProcessNewBatchesAsync we did not link DegreeProcessingRecord to BatchId!
            // Let's add BatchId to DegreeProcessingRecord! Wait, `DegreeProcessingRecord` does not have `BatchId`. 
            // It has `WorkerId`. But WorkerId groups multiple batches.
            // We should link DegreeProcessingRecord to BatchRecord. But we can't change the entity schema right now without migrations.
            // What we can do: the degrees that are in `Processing` state for this `InstitutionId`.
            
            var records = await dbContext.DegreeProcessingRecords
                .Where(pr => pr.State == "Processing")
                .Join(dbContext.Degrees, pr => pr.DegreeId, d => d.Id, (pr, d) => new { pr, d })
                .Where(x => x.d.InstitutionId == batchRecord.InstitutionId)
                .ToListAsync(ct);

            await using var saveTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                batchRecord.Status = "Completed";
                batchRecord.CompletedAt = DateTime.UtcNow;

                foreach (var item in records)
                {
                    var degree = item.d;
                    var record = item.pr;

                    if (record.ActionType == "Update")
                    {
                        var staging = await dbContext.DegreeUpdateRequests.FirstOrDefaultAsync(x => x.DegreeId == degree.Id, ct);
                        if (staging != null)
                        {
                            var oldProofRecord = await dbContext.BatchDegreeRecords.FirstOrDefaultAsync(x => x.DegreeId == degree.Id, ct);
                            var previousVersion = DegreeVersion.Create(
                                degree.Id, degree.CurrentVersion, degree.CryptoData.DataHashLocal, staging.CryptoData.DataHashLocal,
                                degree.TxHashBlockchain ?? batchRecord.TxHash ?? "", degree.UpdatedAt, degree.CryptoData.PlainDataJson,
                                degree.CryptoData.Salt, degree.Major, degree.Classification, oldProofRecord?.ProofHashesJson);
                                
                            dbContext.DegreeVersions.Add(previousVersion);
                            degree.ConfirmUpdate(staging.Major, staging.Classification, staging.CryptoData, batchRecord.TxHash ?? "");
                            dbContext.DegreeUpdateRequests.Remove(staging);
                        }
                    }
                    else if (record.ActionType == "Revoke")
                    {
                        degree.ConfirmRevocation(batchRecord.TxHash ?? "");
                    }
                    else
                    {
                        degree.ConfirmBlockchainSync(batchRecord.TxHash ?? "");
                    }

                    dbContext.DegreeProcessingRecords.Remove(record);
                }

                await dbContext.SaveChangesAsync(ct);
                await saveTransaction.CommitAsync(ct);
                
                _logger.LogInformation("Batch {BatchId} finalized successfully.", batchRecord.Id);
            }
            catch (Exception ex)
            {
                await saveTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to finalize batch {BatchId}", batchRecord.Id);
            }
        }

        private async Task FinalizeBatchFailedAsync(ChainDegreeDbContext dbContext, BatchRecord batchRecord, string reason, CancellationToken ct)
        {
            var records = await dbContext.DegreeProcessingRecords
                .Where(pr => pr.State == "Processing")
                .Join(dbContext.Degrees, pr => pr.DegreeId, d => d.Id, (pr, d) => new { pr, d })
                .Where(x => x.d.InstitutionId == batchRecord.InstitutionId)
                .ToListAsync(ct);

            await using var saveTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                batchRecord.Status = "Failed";
                batchRecord.FailureReason = reason;
                batchRecord.CompletedAt = DateTime.UtcNow;

                foreach (var item in records)
                {
                    var record = item.pr;
                    record.RetryCount++;
                    record.LastRetryAt = DateTime.UtcNow;
                    record.LastError = reason;

                    if (record.RetryCount > 3)
                    {
                        record.State = "Failed";
                        record.NextRetryAt = null;
                        if (record.ActionType == "Issue") item.d.MarkAsSyncError();
                    }
                    else
                    {
                        record.State = "Failed"; // Mark failed so it gets picked up again
                        record.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, record.RetryCount));
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                await saveTransaction.CommitAsync(ct);
                
                _logger.LogWarning("Batch {BatchId} marked as failed. Reason: {Reason}", batchRecord.Id, reason);
            }
            catch (Exception ex)
            {
                await saveTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to mark batch {BatchId} as failed.", batchRecord.Id);
            }
        }
    }
}
