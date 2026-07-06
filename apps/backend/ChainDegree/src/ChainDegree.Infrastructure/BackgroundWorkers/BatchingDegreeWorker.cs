using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees;
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
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChainDegreeDbContext>();
            var degreeRepo = scope.ServiceProvider.GetRequiredService<IDegreeRepository>();
            var merkleTreeService = scope.ServiceProvider.GetRequiredService<IMerkleTreeService>();
            var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

            // 1. Begin database transaction (Transaction Boundary 2)
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            var lockedDegrees = await degreeRepo.GetPendingConfirmationAsync(_options.MaxBatchSize, ct);
            if (lockedDegrees.Count == 0)
            {
                await transaction.RollbackAsync(ct);
                return;
            }

            // Check dual trigger conditions
            var oldestDegree = lockedDegrees.Min(d => d.CreatedAt);
            var waitTimeSeconds = (DateTime.UtcNow - oldestDegree).TotalSeconds;
            var isTriggered = lockedDegrees.Count >= _options.MaxBatchSize || waitTimeSeconds >= _options.MaxWaitTimeSeconds;

            if (!isTriggered)
            {
                // Rollback transaction to release locked rows immediately
                await transaction.RollbackAsync(ct);
                return;
            }

            _logger.LogInformation("[{LogCode}] Dual trigger met. DegreesCount={Count}, WaitTime={WaitTime}s",
                DegreeLogs.Degree_BlockchainSyncStarted.Code,
                lockedDegrees.Count,
                waitTimeSeconds);

            // Group degrees by InstitutionId to process separate batches per institution
            var groups = lockedDegrees.GroupBy(d => d.InstitutionId).ToList();

            foreach (var group in groups)
            {
                var institutionId = group.Key;
                var institutionDegrees = group.ToList();

                // Fetch institution code for batch name generation
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

                // Build Merkle Tree from the leaf hashes
                var leafHashes = institutionDegrees.Select(d => d.CryptoData.DataHashLocal).ToList();
                var treeResult = merkleTreeService.BuildTree(leafHashes);

                // Anchor Merkle root to blockchain
                var txResult = await blockchainService.AnchorMerkleRootAsync(treeResult.MerkleRoot, batchId, ct);

                if (txResult.IsSuccess)
                {
                    batchRecord.Status = "Completed";
                    batchRecord.MerkleRoot = treeResult.MerkleRoot;
                    batchRecord.TxHash = txResult.TxHash;
                    batchRecord.BlockNumber = txResult.BlockNumber;
                    batchRecord.CompletedAt = DateTime.UtcNow;

                    // Update degrees to Confirmed
                    foreach (var degree in institutionDegrees)
                    {
                        degree.ConfirmBlockchainSync(txResult.TxHash!);

                        // Cleanup processing record if exists
                        var processingRecord = await dbContext.DegreeProcessingRecords.FindAsync(degree.Id);
                        if (processingRecord != null)
                        {
                            dbContext.DegreeProcessingRecords.Remove(processingRecord);
                        }
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

                    // Update degrees to error state and manage retry metadata
                    foreach (var degree in institutionDegrees)
                    {
                        degree.MarkAsSyncError();

                        var processingRecord = await dbContext.DegreeProcessingRecords.FindAsync(degree.Id);
                        if (processingRecord == null)
                        {
                            processingRecord = new DegreeProcessingRecord
                            {
                                DegreeId = degree.Id,
                                RetryCount = 0
                            };
                            dbContext.DegreeProcessingRecords.Add(processingRecord);
                        }

                        processingRecord.RetryCount++;
                        processingRecord.LastRetryAt = DateTime.UtcNow;

                        if (processingRecord.RetryCount > 3)
                        {
                            // Permanently fail, do not schedule next retry
                            processingRecord.NextRetryAt = null;
                            _logger.LogError("Degree {Id} has failed syncing after 3 attempts.", degree.Id);
                        }
                        else
                        {
                            // Exponential backoff retry scheduling: 2, 4, 8 minutes
                            var waitMinutes = Math.Pow(2, processingRecord.RetryCount);
                            processingRecord.NextRetryAt = DateTime.UtcNow.AddMinutes(waitMinutes);
                        }
                    }

                    _logger.LogError("[{LogCode}] {Message}. BatchName={BatchName}, Error={Error}",
                        DegreeLogs.Degree_BlockchainSyncFailed.Code,
                        DegreeLogs.Degree_BlockchainSyncFailed.Message,
                        batchName,
                        txResult.ErrorMessage);
                }
            }

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
    }
}
