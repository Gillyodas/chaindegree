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

namespace ChainDegree.Core.Infrastructure.BackgroundWorkers
{
    public class BatchingDegreeWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BatchingWorkerOptions _options;
        private readonly ILogger<BatchingDegreeWorker> _logger;

        public BatchingDegreeWorker(
            IServiceProvider _serviceProvider,
            IOptions<BatchingWorkerOptions> options,
            ILogger<BatchingDegreeWorker> logger)
        {
            this._serviceProvider = _serviceProvider;
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
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in BatchingDegreeWorker execution cycle.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken ct)
        {
            var workerId = Guid.NewGuid().ToString();
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var degreeRepo = scope.ServiceProvider.GetRequiredService<IDegreeRepository>();
            var merkleTreeService = scope.ServiceProvider.GetRequiredService<IMerkleTreeService>();
            var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

            // 1. Begin DB transaction to claim records atomically
            await using (var claimTransaction = await dbContext.Database.BeginTransactionAsync(ct))
            {
                var lockedDegrees = await degreeRepo.GetPendingConfirmationAsync(_options.MaxBatchSize, ct);
                if (lockedDegrees.Count == 0)
                {
                    await claimTransaction.RollbackAsync(ct);
                    return;
                }

                // Check dual trigger conditions
                var oldestDegree = lockedDegrees.Min(d => d.CreatedAt);
                var waitTimeSeconds = (DateTime.UtcNow - oldestDegree).TotalSeconds;
                var isTriggered = lockedDegrees.Count >= _options.MaxBatchSize || waitTimeSeconds >= _options.MaxWaitTimeSeconds;

                if (!isTriggered)
                {
                    await claimTransaction.RollbackAsync(ct);
                    return;
                }

                _logger.LogInformation("[{LogCode}] Claiming queued records. DegreesCount={Count}, WaitTime={WaitTime}s",
                    DegreeLogs.Degree_BlockchainSyncStarted.Code,
                    lockedDegrees.Count,
                    waitTimeSeconds);

                // Transition states in DegreeProcessingRecords to Processing (Claiming them)
                foreach (var degree in lockedDegrees)
                {
                    var pr = await dbContext.DegreeProcessingRecords.FindAsync(degree.Id);
                    if (pr == null)
                    {
                        pr = new DegreeProcessingRecord
                        {
                            DegreeId = degree.Id,
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
                    pr.LeaseUntil = DateTime.UtcNow.AddMinutes(10);
                }

                await dbContext.SaveChangesAsync(ct);
                await claimTransaction.CommitAsync(ct);
            }

            // 2. Process batches per institution (outside database lock holding)
            var claimedDegrees = await dbContext.Degrees
                .Include(d => d.CryptoData)
                .Join(dbContext.DegreeProcessingRecords,
                    d => d.Id,
                    pr => pr.DegreeId,
                    (d, pr) => new { Degree = d, Record = pr })
                .Where(x => x.Record.WorkerId == workerId && x.Record.State == "Processing")
                .ToListAsync(ct);

            if (claimedDegrees.Count == 0) return;

            var groups = claimedDegrees.GroupBy(x => x.Degree.InstitutionId).ToList();

            foreach (var group in groups)
            {
                var institutionId = group.Key;
                var institutionItems = group.ToList();
                var institutionDegrees = institutionItems.Select(x => x.Degree).ToList();

                var institution = await dbContext.EducationInstitutions
                    .FirstOrDefaultAsync(x => x.Id == institutionId, ct);
                var instCode = institution?.Code ?? "UNKNOWN";
                var batchId = Guid.NewGuid();
                var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8);
                var batchName = $"BATCH_{instCode.ToUpperInvariant()}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{shortGuid}";

                var batchRecord = new BatchRecord
                {
                    Id = batchId,
                    InstitutionId = institutionId,
                    BatchName = batchName,
                    Status = "Processing",
                    DegreeCount = institutionDegrees.Count,
                    EstimatedWaitTimeSeconds = _options.MaxWaitTimeSeconds,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.BatchRecords.Add(batchRecord);
                await dbContext.SaveChangesAsync(ct);

                // Build leaf hashes taking updates into account
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
                var txResult = await blockchainService.AnchorMerkleRootAsync(treeResult.MerkleRoot, batchId, ct);

                await using var saveTransaction = await dbContext.Database.BeginTransactionAsync(ct);
                try
                {
                    if (txResult.IsSuccess)
                    {
                        batchRecord.Status = "Completed";
                        batchRecord.MerkleRoot = treeResult.MerkleRoot;
                        batchRecord.TxHash = txResult.TxHash;
                        batchRecord.BlockNumber = txResult.BlockNumber;
                        batchRecord.CompletedAt = DateTime.UtcNow;

                        // Atomic Transaction Workflow
                        for (int i = 0; i < institutionItems.Count; i++)
                        {
                            var item = institutionItems[i];
                            var degree = item.Degree;
                            var record = item.Record;

                            if (record.ActionType == "Update")
                            {
                                var staging = await dbContext.DegreeUpdateRequests.FirstOrDefaultAsync(x => x.DegreeId == degree.Id, ct);
                                if (staging != null)
                                {
                                    // 1. Insert DegreeVersion (Lưu bản cũ)
                                    var previousVersion = DegreeVersion.Create(
                                        degree.Id,
                                        degree.CurrentVersion,
                                        degree.CryptoData.DataHashLocal,
                                        staging.CryptoData.DataHashLocal,
                                        degree.TxHashBlockchain ?? txResult.TxHash!,
                                        degree.UpdatedAt
                                    );
                                    dbContext.DegreeVersions.Add(previousVersion);

                                    // 2. Update Degree (Version mới)
                                    degree.ConfirmUpdate(staging.Major, staging.Classification, staging.CryptoData, txResult.TxHash!);

                                    // 3. Delete staging model
                                    dbContext.DegreeUpdateRequests.Remove(staging);
                                }
                            }
                            else if (record.ActionType == "Revoke")
                            {
                                degree.ConfirmRevocation(txResult.TxHash!);
                            }
                            else
                            {
                                degree.ConfirmBlockchainSync(txResult.TxHash!);
                            }

                            dbContext.DegreeProcessingRecords.Remove(record);
                        }

                        // Save Merkle Proofs for each degree
                        foreach (var proof in treeResult.Proofs)
                        {
                            var degree = institutionDegrees[proof.LeafIndex];
                            var proofRecord = new BatchDegreeRecord
                            {
                                BatchId = batchId,
                                DegreeId = degree.Id,
                                LeafIndex = proof.LeafIndex,
                                ProofHashesJson = System.Text.Json.JsonSerializer.Serialize(proof.ProofHashes)
                            };
                            dbContext.BatchDegreeRecords.Add(proofRecord);
                        }

                        _logger.LogInformation("[{LogCode}] {Message}. BatchName={BatchName}, TxHash={TxHash}",
                            DegreeLogs.Degree_BlockchainSyncCompleted.Code,
                            DegreeLogs.Degree_BlockchainSyncCompleted.Message,
                            batchName,
                            txResult.TxHash);
                    }
                    else
                    {
                        batchRecord.Status = "Failed";
                        batchRecord.FailureReason = txResult.ErrorMessage;
                        batchRecord.CompletedAt = DateTime.UtcNow;

                        // Manage failure and retries
                        foreach (var item in institutionItems)
                        {
                            var degree = item.Degree;
                            var record = item.Record;

                            record.RetryCount++;
                            record.LastRetryAt = DateTime.UtcNow;
                            record.LastError = txResult.ErrorMessage;

                            if (record.RetryCount > 3)
                            {
                                record.State = "Failed";
                                record.NextRetryAt = null;
                                
                                if (record.ActionType == "Issue")
                                {
                                    degree.MarkAsSyncError();
                                }
                                _logger.LogError("Degree action {Action} for {Id} permanently failed after 3 retries.", record.ActionType, degree.Id);
                            }
                            else
                            {
                                record.State = "Failed";
                                var waitMinutes = Math.Pow(2, record.RetryCount);
                                record.NextRetryAt = DateTime.UtcNow.AddMinutes(waitMinutes);
                                if (record.ActionType == "Issue")
                                {
                                    degree.MarkAsSyncError();
                                }
                            }
                        }

                        _logger.LogError("[{LogCode}] {Message}. BatchName={BatchName}, Error={Error}",
                            DegreeLogs.Degree_BlockchainSyncFailed.Code,
                            DegreeLogs.Degree_BlockchainSyncFailed.Message,
                            batchName,
                            txResult.ErrorMessage);
                    }

                    await dbContext.SaveChangesAsync(ct);
                    await saveTransaction.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    await saveTransaction.RollbackAsync(ct);
                    _logger.LogError(ex, "Failed to persist batch execution results for batch {BatchName}", batchName);
                    throw;
                }
            }
        }
    }
}
