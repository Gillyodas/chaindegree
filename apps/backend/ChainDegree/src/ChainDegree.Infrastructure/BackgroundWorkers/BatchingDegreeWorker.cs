using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using ChainDegree.Core.Infrastructure.Monitoring;
using ChainDegree.Core.Infrastructure.Persistence;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using ChainDegree.SharedKernel.Common.Error;
using ChainDegree.SharedKernel.DomainErrors.Blockchain;
using ChainDegree.SharedKernel.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChainDegree.Core.Infrastructure.BackgroundWorkers
{
    public class BatchingDegreeWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BatchingWorkerOptions _options;
        private readonly ILogger<BatchingDegreeWorker> _logger;
        private readonly WorkerMetrics? _metrics;

        public BatchingDegreeWorker(
            IServiceProvider serviceProvider,
            IOptions<BatchingWorkerOptions> options,
            ILogger<BatchingDegreeWorker> logger,
            WorkerMetrics? metrics = null)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
            _metrics = metrics;
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
                var batchCorrelationId = Guid.NewGuid().ToString();
                using var batchScope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["BatchCorrelationId"] = batchCorrelationId,
                    ["BatchId"] = batch.Id,
                    ["BlockchainTxHash"] = batch.TxHash ?? string.Empty
                });

                var recoverySw = Stopwatch.StartNew();

                try
                {
                    bool isConfirmed = false;
                    string? failedReason = null;

                    if (!string.IsNullOrEmpty(batch.TxHash))
                    {
                        var statusResult = await blockchainService.GetTransactionStatusAsync(batch.TxHash, ct);
                        
                        if (statusResult.IsFailure)
                        {
                            _logger.LogError("Failed to get transaction status for batch {BatchId}, TxHash={BlockchainTxHash}, BatchCorrelationId={BatchCorrelationId}: {Error}",
                                batch.Id, batch.TxHash, batchCorrelationId, statusResult.Error.Message);
                            
                            if (IsTransientError(statusResult.Error))
                            {
                                _metrics?.RetryCount.Inc();
                                continue;
                            }
                            
                            failedReason = $"Transaction lookup failed permanently: {statusResult.Error.Message}";
                        }
                        else
                        {
                            var status = statusResult.Value;
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
                                var waitTime = DateTime.UtcNow - batch.CreatedAt;
                                if (waitTime.TotalMinutes > 30)
                                {
                                    failedReason = $"Transaction {status} timeout after 30 minutes.";
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Batch {BatchId} has null TxHash. Checking on-chain state... BatchCorrelationId={BatchCorrelationId}", batch.Id, batchCorrelationId);
                        
                        var batchResult = await blockchainService.GetBatchAsync(batch.Id.ToString(), ct);
                        
                        if (batchResult.IsSuccess)
                        {
                            if (batchResult.Value.Exists)
                            {
                                isConfirmed = true;
                                batch.TxHash = "UNKNOWN_RECOVERED_ONCHAIN";
                            }
                            else
                            {
                                failedReason = "Unknown outcome resolved: Batch not found on-chain. Will retry.";
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed to query batch metadata for batch {BatchId}, BatchCorrelationId={BatchCorrelationId}: {Error}",
                                batch.Id, batchCorrelationId, batchResult.Error.Message);
                            
                            if (IsTransientError(batchResult.Error))
                            {
                                _metrics?.RetryCount.Inc();
                                continue;
                            }
                            
                            failedReason = $"Batch metadata lookup failed permanently: {batchResult.Error.Message}";
                        }
                    }

                    recoverySw.Stop();

                    if (isConfirmed)
                    {
                        _logger.LogInformation("Batch {BatchId} recovered as Confirmed. TxHash={BlockchainTxHash}, ElapsedMs={ElapsedMs}",
                            batch.Id, batch.TxHash, recoverySw.ElapsedMilliseconds);
                        await FinalizeBatchSuccessAsync(dbContext, batch, ct);
                    }
                    else if (failedReason != null)
                    {
                        _logger.LogWarning("Batch {BatchId} failed recovery. Reason={Reason}, ElapsedMs={ElapsedMs}",
                            batch.Id, failedReason, recoverySw.ElapsedMilliseconds);
                        await FinalizeBatchFailedAsync(dbContext, batch, failedReason, ct);
                    }
                }
                catch (Exception ex)
                {
                    recoverySw.Stop();
                    _logger.LogError(ex, "Failed to recover pending batch {BatchId}, BatchCorrelationId={BatchCorrelationId}, ElapsedMs={ElapsedMs}",
                        batch.Id, batchCorrelationId, recoverySw.ElapsedMilliseconds);
                }
            }
        }

        private async Task ProcessNewBatchesAsync(CancellationToken ct)
        {
            var workerId = Guid.NewGuid().ToString();
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var merkleTreeService = scope.ServiceProvider.GetRequiredService<IMerkleTreeService>();
            var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

            var now = DateTime.UtcNow;

            var totalQueueCount = await dbContext.Degrees
                .CountAsync(d => d.Status == StatusEnum.Pending_Confirmation || d.Status == StatusEnum.Pending_Update || d.Status == StatusEnum.Pending_Revocation, ct);
            _metrics?.QueueLength.Set(totalQueueCount);

            var orphanedLeases = await dbContext.DegreeProcessingRecords
                .CountAsync(pr => pr.State == "Processing" && pr.LeaseUntil != null && pr.LeaseUntil < now, ct);
            _metrics?.LeaseOrphanCount.Set(orphanedLeases);

            await using var claimTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            
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
                var batchCorrelationId = Guid.NewGuid().ToString();

                using var batchScope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["BatchCorrelationId"] = batchCorrelationId,
                    ["BatchId"] = batchId,
                    ["InstitutionId"] = institutionId
                });

                var totalStopwatch = Stopwatch.StartNew();

                _logger.LogInformation("Processing batch {BatchId} with {DegreeCount} degrees. BatchCorrelationId={BatchCorrelationId}",
                    batchId, institutionDegrees.Count, batchCorrelationId);

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

                var merkleStopwatch = Stopwatch.StartNew();
                var treeResult = merkleTreeService.BuildTree(leafHashes);
                merkleStopwatch.Stop();

                _metrics?.MerkleBuildTime.Observe(merkleStopwatch.Elapsed.TotalSeconds);
                _logger.LogInformation("Merkle tree built for batch {BatchId}. Root={MerkleRoot}, LeafCount={LeafCount}, ElapsedMs={ElapsedMs}",
                    batchId, treeResult.MerkleRoot, leafHashes.Count, merkleStopwatch.ElapsedMilliseconds);

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

                Result<AnchorResult>? anchorResult = null;
                string? txError = null;

                var txStopwatch = Stopwatch.StartNew();
                int maxRetries = 3;
                for (int retry = 0; retry < maxRetries; retry++)
                {
                    var actionType = institutionItems.First().Record.ActionType;
                    var result = await blockchainService.AnchorMerkleRootAsync(
                        batchId.ToString(), 
                        treeResult.MerkleRoot, 
                        institutionId.ToString(), 
                        actionType, 
                        ct);
                    
                    if (result.IsSuccess)
                    {
                        anchorResult = result;
                        break;
                    }
                    
                    txError = result.Error.Message;
                    
                    if (IsTransientError(result.Error) && retry < maxRetries - 1)
                    {
                        _metrics?.RetryCount.Inc();
                        _logger.LogWarning("Transient failure in AnchorMerkleRoot for batch {BatchId}. Retrying attempt {Attempt}. Reason={Reason}",
                            batchId, retry + 1, txError);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retry)), ct);
                        continue;
                    }
                    
                    break;
                }

                txStopwatch.Stop();
                _metrics?.BlockchainTxTime.Observe(txStopwatch.Elapsed.TotalSeconds);

                totalStopwatch.Stop();

                if (anchorResult != null && anchorResult.IsSuccess)
                {
                    var txHash = anchorResult.Value.TransactionHash;
                    batchRecord.TxHash = txHash;
                    await dbContext.SaveChangesAsync(ct);

                    _metrics?.BatchLatency.Observe(totalStopwatch.Elapsed.TotalSeconds);
                    _metrics?.BatchesProcessed.Inc();

                    _logger.LogInformation("Batch {BatchId} confirmed. TxHash={BlockchainTxHash}, TotalElapsedMs={ElapsedMs}",
                        batchId, txHash, totalStopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _metrics?.BatchesFailed.Inc();

                    if (anchorResult != null && !IsTransientError(anchorResult.Error))
                    {
                        _logger.LogError("Permanent failure executing AnchorMerkleRoot for batch {BatchId}: {Error}", batchId, txError);
                        await FinalizeBatchFailedAsync(dbContext, batchRecord, anchorResult.Error.Message, ct);
                    }
                    else
                    {
                        _logger.LogWarning("Unknown Outcome (Transient Timeout) for batch {BatchId}: {Error}", batchId, txError);
                    }
                }
            }
        }

        private bool IsTransientError(Error error)
        {
            return error == BlockchainErrors.NetworkTimeout 
                || error == BlockchainErrors.RpcUnavailable;
        }

        private async Task FinalizeBatchSuccessAsync(ChainDegreeDbContext dbContext, BatchRecord batchRecord, CancellationToken ct)
        {
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
                
                _logger.LogInformation("Batch {BatchId} finalized successfully. TxHash={BlockchainTxHash}", batchRecord.Id, batchRecord.TxHash);
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
                        record.State = "Failed";
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
