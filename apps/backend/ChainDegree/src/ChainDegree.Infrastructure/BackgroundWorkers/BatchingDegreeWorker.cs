using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
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
    public sealed record BatchJobContext(
        Guid BatchId,
        Guid InstitutionId,
        string BatchName,
        string MerkleRoot,
        string ActionType,
        string LeaseId,
        List<Guid> DegreeIds
    );

    public class BatchingDegreeWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BatchingWorkerOptions _options;
        private readonly INonceManager _nonceManager;
        private readonly ILogger<BatchingDegreeWorker> _logger;
        private readonly WorkerMetrics? _metrics;
        private Channel<BatchJobContext>? _channel;

        public BatchingDegreeWorker(
            IServiceProvider serviceProvider,
            IOptions<BatchingWorkerOptions> options,
            INonceManager nonceManager,
            ILogger<BatchingDegreeWorker> logger,
            WorkerMetrics? metrics = null)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _nonceManager = nonceManager;
            _logger = logger;
            _metrics = metrics;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BatchingDegreeWorker starting. Polling={Interval}s, Consumers={Consumers}, ChannelCapacity={Capacity}.",
                _options.PollingIntervalSeconds, _options.ConsumerCount, _options.ChannelCapacity);

            // Initialize NonceManager from chain
            try
            {
                await _nonceManager.InitializeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NonceManager initialization failed on startup. Will retry on demand.");
            }

            // Create Bounded Channel for backpressure
            var channelOptions = new BoundedChannelOptions(_options.ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false
            };
            _channel = Channel.CreateBounded<BatchJobContext>(channelOptions);

            var workerTasks = new List<Task>();

            // Producer Task
            workerTasks.Add(ProduceBatchesAsync(stoppingToken));

            // Concurrent Consumer Tasks
            for (int i = 0; i < _options.ConsumerCount; i++)
            {
                int consumerId = i + 1;
                workerTasks.Add(ConsumeBatchesAsync(consumerId, stoppingToken));
            }

            await Task.WhenAll(workerTasks);
        }

        private async Task ProduceBatchesAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await RecoverPendingBatchesAsync(ct);
                    await ProcessNewBatchesAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Producer loop of BatchingDegreeWorker.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), ct);
            }

            _channel?.Writer.TryComplete();
        }

        private async Task ConsumeBatchesAsync(int consumerId, CancellationToken ct)
        {
            _logger.LogInformation("Consumer {ConsumerId} started.", consumerId);

            if (_channel == null) return;

            await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
                var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

                using var batchScope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["BatchId"] = job.BatchId,
                    ["InstitutionId"] = job.InstitutionId,
                    ["ConsumerId"] = consumerId,
                    ["LeaseId"] = job.LeaseId
                });

                var txStopwatch = Stopwatch.StartNew();

                try
                {
                    // Reserve Nonce atomically
                    long nonce = await _nonceManager.ReserveNonceAsync(ct);
                    _logger.LogInformation("Consumer {ConsumerId} reserved Nonce {Nonce} for batch {BatchId}.", consumerId, nonce, job.BatchId);

                    var batchRecord = await dbContext.BatchRecords.FirstOrDefaultAsync(b => b.Id == job.BatchId, ct);
                    if (batchRecord == null)
                    {
                        _logger.LogWarning("Batch record {BatchId} not found in DB.", job.BatchId);
                        continue;
                    }

                    var result = await blockchainService.AnchorMerkleRootAsync(
                        job.BatchId.ToString(),
                        job.MerkleRoot,
                        job.InstitutionId.ToString(),
                        job.ActionType,
                        ct);

                    txStopwatch.Stop();
                    _metrics?.BlockchainTxTime.Observe(txStopwatch.Elapsed.TotalSeconds);

                    if (result.IsSuccess)
                    {
                        var txHash = result.Value.TransactionHash;
                        batchRecord.TxHash = txHash;
                        batchRecord.Status = BatchStatus.Submitted;
                        await dbContext.SaveChangesAsync(ct);

                        _logger.LogInformation("Batch {BatchId} submitted to blockchain. TxHash={TxHash}, ElapsedMs={ElapsedMs}",
                            job.BatchId, txHash, txStopwatch.ElapsedMilliseconds);

                        // Poll receipt or finalize
                        await FinalizeBatchSuccessWithFencingAsync(dbContext, batchRecord, job.LeaseId, ct);
                    }
                    else
                    {
                        _metrics?.BatchesFailed.Inc();

                        if (IsTransientError(result.Error))
                        {
                            _logger.LogWarning("Transient error (RPC Timeout) submitting batch {BatchId}: {Error}. Setting status to Unknown.", job.BatchId, result.Error.Message);
                            batchRecord.Status = BatchStatus.Unknown;
                            batchRecord.FailureReason = result.Error.Message;
                            await dbContext.SaveChangesAsync(ct);

                            // Trigger Nonce resync
                            await _nonceManager.ResyncAsync(ct);
                        }
                        else
                        {
                            _logger.LogError("Permanent failure submitting batch {BatchId}: {Error}", job.BatchId, result.Error.Message);
                            await FinalizeBatchFailedWithFencingAsync(dbContext, batchRecord, job.LeaseId, result.Error.Message, ct);
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    txStopwatch.Stop();
                    _logger.LogError(ex, "Error in Consumer {ConsumerId} processing batch {BatchId}.", consumerId, job.BatchId);
                }
            }
        }

        private async Task ProcessNewBatchesAsync(CancellationToken ct)
        {
            var workerId = Guid.NewGuid().ToString();
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var merkleTreeService = scope.ServiceProvider.GetRequiredService<IMerkleTreeService>();

            var now = DateTime.UtcNow;

            var totalQueueCount = await dbContext.Degrees
                .CountAsync(d => d.Status == StatusEnum.Pending_Confirmation || d.Status == StatusEnum.Pending_Update || d.Status == StatusEnum.Pending_Revocation, ct);
            _metrics?.QueueLength.Set(totalQueueCount);

            var orphanedLeases = await dbContext.DegreeProcessingRecords
                .CountAsync(pr => pr.State == DegreeProcessingState.Processing && pr.LeaseUntil != null && pr.LeaseUntil < now, ct);
            _metrics?.LeaseOrphanCount.Set(orphanedLeases);

            await using var claimTransaction = await dbContext.Database.BeginTransactionAsync(ct);

            var availableDegreeIds = await dbContext.Degrees
                .Where(d => d.Status == StatusEnum.Pending_Confirmation || d.Status == StatusEnum.Pending_Update || d.Status == StatusEnum.Pending_Revocation)
                .GroupJoin(dbContext.DegreeProcessingRecords, d => d.Id, pr => pr.DegreeId, (d, prs) => new { d, prs })
                .SelectMany(x => x.prs.DefaultIfEmpty(), (x, pr) => new { x.d, pr })
                .Where(x => x.pr == null ||
                            (x.pr.State == DegreeProcessingState.Queued) ||
                            (x.pr.State == DegreeProcessingState.Failed && x.pr.NextRetryAt != null && x.pr.NextRetryAt <= now))
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

            var leaseId = Guid.NewGuid().ToString();
            var leaseUntil = now.AddMinutes(_options.LeaseDurationMinutes);

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
                        State = DegreeProcessingState.Queued,
                        RetryCount = 0
                    };
                    dbContext.DegreeProcessingRecords.Add(pr);
                }

                pr.State = DegreeProcessingState.Processing;
                pr.WorkerId = workerId;
                pr.LeaseId = leaseId;
                pr.LeaseUntil = leaseUntil;
            }

            await dbContext.SaveChangesAsync(ct);
            await claimTransaction.CommitAsync(ct);

            var claimedDegrees = await dbContext.Degrees
                .Include(d => d.CryptoData)
                .Join(dbContext.DegreeProcessingRecords, d => d.Id, pr => pr.DegreeId, (d, pr) => new { Degree = d, Record = pr })
                .Where(x => x.Record.WorkerId == workerId && x.Record.State == DegreeProcessingState.Processing && x.Record.LeaseId == leaseId)
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
                    Status = BatchStatus.Processing,
                    DegreeCount = institutionDegrees.Count,
                    MerkleRoot = treeResult.MerkleRoot,
                    EstimatedWaitTimeSeconds = _options.MaxWaitTimeSeconds,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.BatchRecords.Add(batchRecord);
                await dbContext.SaveChangesAsync(ct);

                var actionType = institutionItems.First().Record.ActionType;
                var job = new BatchJobContext(
                    batchId,
                    institutionId,
                    batchName,
                    treeResult.MerkleRoot,
                    actionType,
                    leaseId,
                    institutionDegrees.Select(d => d.Id).ToList()
                );

                if (_channel != null)
                {
                    await _channel.Writer.WriteAsync(job, ct);
                }
            }
        }

        private async Task RecoverPendingBatchesAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

            var pendingBatches = await dbContext.BatchRecords
                .Where(b => b.Status == BatchStatus.Processing || b.Status == BatchStatus.Submitted || b.Status == BatchStatus.Unknown)
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
                            if (IsTransientError(statusResult.Error))
                            {
                                _metrics?.RetryCount.Inc();
                                continue;
                            }
                            failedReason = $"Transaction lookup failed: {statusResult.Error.Message}";
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
                        var batchResult = await blockchainService.GetBatchAsync(batch.Id.ToString(), ct);

                        if (batchResult.IsSuccess && batchResult.Value.Exists)
                        {
                            isConfirmed = true;
                            batch.TxHash = "UNKNOWN_RECOVERED_ONCHAIN";
                        }
                        else
                        {
                            var waitTime = DateTime.UtcNow - batch.CreatedAt;
                            if (waitTime.TotalMinutes > 30)
                            {
                                failedReason = "Batch not found on-chain after timeout.";
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    recoverySw.Stop();

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
                    recoverySw.Stop();
                    _logger.LogError(ex, "Failed to recover pending batch {BatchId}", batch.Id);
                }
            }
        }

        private async Task FinalizeBatchSuccessWithFencingAsync(ChainDegreeDbContext dbContext, BatchRecord batchRecord, string leaseId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var records = await dbContext.DegreeProcessingRecords
                .Where(pr => pr.State == DegreeProcessingState.Processing && pr.LeaseId == leaseId && (pr.LeaseUntil == null || pr.LeaseUntil > now))
                .Join(dbContext.Degrees, pr => pr.DegreeId, d => d.Id, (pr, d) => new { pr, d })
                .Where(x => x.d.InstitutionId == batchRecord.InstitutionId)
                .ToListAsync(ct);

            if (records.Count == 0)
            {
                _logger.LogWarning("Fencing Token check failed for Batch {BatchId}, LeaseId {LeaseId}. Lease expired or ownership lost.", batchRecord.Id, leaseId);
                return;
            }

            await using var saveTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                batchRecord.Status = BatchStatus.Completed;
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

                _metrics?.BatchLatency.Observe((DateTime.UtcNow - batchRecord.CreatedAt).TotalSeconds);
                _metrics?.BatchesProcessed.Inc();

                _logger.LogInformation("Batch {BatchId} finalized successfully with Fencing Token {LeaseId}.", batchRecord.Id, leaseId);
            }
            catch (Exception ex)
            {
                await saveTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to finalize batch {BatchId} with Fencing Token.", batchRecord.Id);
            }
        }

        private async Task FinalizeBatchSuccessAsync(ChainDegreeDbContext dbContext, BatchRecord batchRecord, CancellationToken ct)
        {
            var records = await dbContext.DegreeProcessingRecords
                .Where(pr => pr.State == DegreeProcessingState.Processing || pr.State == DegreeProcessingState.Submitted || pr.State == DegreeProcessingState.Unknown)
                .Join(dbContext.Degrees, pr => pr.DegreeId, d => d.Id, (pr, d) => new { pr, d })
                .Where(x => x.d.InstitutionId == batchRecord.InstitutionId)
                .ToListAsync(ct);

            await using var saveTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                batchRecord.Status = BatchStatus.Completed;
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

                _logger.LogInformation("Batch {BatchId} finalized successfully via recovery. TxHash={TxHash}", batchRecord.Id, batchRecord.TxHash);
            }
            catch (Exception ex)
            {
                await saveTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to finalize batch {BatchId} via recovery", batchRecord.Id);
            }
        }

        private async Task FinalizeBatchFailedWithFencingAsync(ChainDegreeDbContext dbContext, BatchRecord batchRecord, string leaseId, string reason, CancellationToken ct)
        {
            var records = await dbContext.DegreeProcessingRecords
                .Where(pr => pr.State == DegreeProcessingState.Processing && pr.LeaseId == leaseId)
                .Join(dbContext.Degrees, pr => pr.DegreeId, d => d.Id, (pr, d) => new { pr, d })
                .Where(x => x.d.InstitutionId == batchRecord.InstitutionId)
                .ToListAsync(ct);

            await using var saveTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                batchRecord.Status = BatchStatus.Failed;
                batchRecord.FailureReason = reason;
                batchRecord.CompletedAt = DateTime.UtcNow;

                foreach (var item in records)
                {
                    var record = item.pr;
                    record.RetryCount++;
                    record.LastRetryAt = DateTime.UtcNow;
                    record.LastError = reason;

                    if (record.RetryCount >= 3)
                    {
                        record.State = DegreeProcessingState.Failed;
                        record.NextRetryAt = null;
                        item.d.MarkAsSyncError();
                    }
                    else
                    {
                        record.State = DegreeProcessingState.Failed;
                        record.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, record.RetryCount));
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                await saveTransaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await saveTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to mark batch {BatchId} as failed.", batchRecord.Id);
            }
        }

        private async Task FinalizeBatchFailedAsync(ChainDegreeDbContext dbContext, BatchRecord batchRecord, string reason, CancellationToken ct)
        {
            var records = await dbContext.DegreeProcessingRecords
                .Where(pr => pr.State == DegreeProcessingState.Processing || pr.State == DegreeProcessingState.Submitted || pr.State == DegreeProcessingState.Unknown)
                .Join(dbContext.Degrees, pr => pr.DegreeId, d => d.Id, (pr, d) => new { pr, d })
                .Where(x => x.d.InstitutionId == batchRecord.InstitutionId)
                .ToListAsync(ct);

            await using var saveTransaction = await dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                batchRecord.Status = BatchStatus.Failed;
                batchRecord.FailureReason = reason;
                batchRecord.CompletedAt = DateTime.UtcNow;

                foreach (var item in records)
                {
                    var record = item.pr;
                    record.RetryCount++;
                    record.LastRetryAt = DateTime.UtcNow;
                    record.LastError = reason;

                    if (record.RetryCount >= 3)
                    {
                        record.State = DegreeProcessingState.Failed;
                        record.NextRetryAt = null;
                        item.d.MarkAsSyncError();
                    }
                    else
                    {
                        record.State = DegreeProcessingState.Failed;
                        record.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, record.RetryCount));
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                await saveTransaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await saveTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to mark batch {BatchId} as failed via recovery.", batchRecord.Id);
            }
        }

        private bool IsTransientError(Error error)
        {
            return error == BlockchainErrors.NetworkTimeout
                || error == BlockchainErrors.RpcUnavailable;
        }
    }
}
