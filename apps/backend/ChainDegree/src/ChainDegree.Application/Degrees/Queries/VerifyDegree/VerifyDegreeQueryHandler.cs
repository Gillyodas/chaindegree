using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.SharedKernel.Enums;
using ChainDegree.SharedKernel.DomainErrors.Degrees.Degree;
using ChainDegree.SharedKernel.Result;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Application.Degrees.Queries.VerifyDegree
{
    public class VerifyDegreeQueryHandler : IRequestHandler<VerifyDegreeQuery, Result<VerifyDegreeResponse>>
    {
        private readonly IDegreeRepository _degreeRepository;
        private readonly IBlockchainService _blockchainService;
        private readonly IMerkleTreeService _merkleTreeService;
        private readonly IDegreeHashService _degreeHashService;
        private readonly IJsonCanonicalizer _canonicalizer;
        private readonly IHashService _hashService;
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly ILogger<VerifyDegreeQueryHandler> _logger;

        public VerifyDegreeQueryHandler(
            IDegreeRepository degreeRepository,
            IBlockchainService blockchainService,
            IMerkleTreeService merkleTreeService,
            IDegreeHashService degreeHashService,
            IJsonCanonicalizer canonicalizer,
            IHashService hashService,
            IBehaviorLogService behaviorLogService,
            ILogger<VerifyDegreeQueryHandler> logger)
        {
            _degreeRepository = degreeRepository;
            _blockchainService = blockchainService;
            _merkleTreeService = merkleTreeService;
            _degreeHashService = degreeHashService;
            _canonicalizer = canonicalizer;
            _hashService = hashService;
            _behaviorLogService = behaviorLogService;
            _logger = logger;
        }

        public async Task<Result<VerifyDegreeResponse>> Handle(VerifyDegreeQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Verification request received for DegreeCode={DegreeCode}, Version={Version}, IsDirectDataMode={IsDirectDataMode}",
                request.DegreeCode, request.Version, request.IsDirectDataMode);

            // 1. Resolve snapshot
            var snapshot = await _degreeRepository.GetVerificationSnapshotAsync(request.DegreeCode, request.Version, ct);
            if (snapshot == null)
            {
                if (request.Version.HasValue)
                {
                    _logger.LogWarning("Unsupported version requested for DegreeCode={DegreeCode}, Version={Version}", request.DegreeCode, request.Version);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.UnsupportedVersion);
                }
                else
                {
                    _logger.LogWarning("Degree not found for DegreeCode={DegreeCode}", request.DegreeCode);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.NotFound);
                }
            }

            // Cross-check IssuedAt if provided
            if (request.IssuedAt.HasValue && snapshot.IssuedAt.Date != request.IssuedAt.Value.Date)
            {
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
            }

            // 2. Status Check
            if (snapshot.Status == StatusEnum.Revoked)
            {
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.Revoked, ct);
                return Result<VerifyDegreeResponse>.Success(new VerifyDegreeResponse(
                    Verified: false,
                    Status: "Revoked",
                    VerificationSource: null,
                    DegreeCode: request.DegreeCode,
                    Version: snapshot.Version,
                    InstitutionName: snapshot.InstitutionName,
                    StudentFullName: snapshot.StudentFullName,
                    Major: snapshot.Major,
                    Classification: snapshot.Classification,
                    IssuedAt: snapshot.IssuedAt,
                    Blockchain: null
                ));
            }

            // 3. Local Integrity Check
            string recalculatedHash;

            if (request.IsDirectDataMode)
            {
                // Direct Data Mode validation: validate salt format (16 hex characters)
                if (string.IsNullOrWhiteSpace(request.Salt)
                    || request.Salt.Length != 16
                    || !request.Salt.All(c => Uri.IsHexDigit(c)))
                {
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.InvalidSaltFormat);
                }

                // Canonicalize input JSON before hashing
                JsonNode? jsonNode;
                try
                {
                    jsonNode = JsonNode.Parse(request.PlainDataJson!);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse PlainDataJson for Direct Data Mode verification.");
                    await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
                }

                if (jsonNode == null)
                {
                    await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
                }

                var canonResult = _canonicalizer.Canonicalize(jsonNode);
                if (canonResult.IsFailure)
                {
                    _logger.LogError("Canonicalization failed for Direct Data Mode verification.");
                    await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
                }

                var hashResult = _hashService.HashData(canonResult.Value, request.Salt);
                if (hashResult.IsFailure)
                {
                    _logger.LogError("Hash calculation failed for Direct Data Mode verification.");
                    await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
                }

                recalculatedHash = hashResult.Value;
            }
            else
            {
                // QR Payload Mode validation
                var originalIssuedAt = snapshot.IssuedAt;
                try
                {
                    using var doc = JsonDocument.Parse(snapshot.PlainDataJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("issuedAt", out var prop) && DateTime.TryParse(prop.GetString(), out var parsedDate))
                    {
                        originalIssuedAt = parsedDate;
                    }
                    else if (root.TryGetProperty("IssuedAt", out var prop2) && DateTime.TryParse(prop2.GetString(), out var parsedDate2))
                    {
                        originalIssuedAt = parsedDate2;
                    }
                }
                catch { }

                var degreeData = new DegreeData(
                    request.DegreeCode,
                    snapshot.StudentId,
                    snapshot.Major,
                    snapshot.Classification,
                    originalIssuedAt
                );

                try
                {
                    recalculatedHash = await _degreeHashService.CalculateHashAsync(degreeData, snapshot.Salt, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recalculate degree hash.");
                    await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
                }
            }

            if (!string.Equals(recalculatedHash, snapshot.DataHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Local integrity check failed for DegreeCode={DegreeCode}. Expected={Expected}, Computed={Computed}",
                    request.DegreeCode, snapshot.DataHash, recalculatedHash);
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
            }

            // 4. Blockchain Integrity Check
            if (snapshot.Status != StatusEnum.Confirmed || string.IsNullOrEmpty(snapshot.TxHash))
            {
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            // 4.1 Lookup BatchId from DB using DegreeId
            var batchId = await _degreeRepository.GetBatchIdByDegreeIdAsync(snapshot.DegreeId, ct);
            if (batchId == null)
            {
                _logger.LogWarning("Batch ID not found for DegreeId={DegreeId}", snapshot.DegreeId);
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            // 4.2 Fetch Merkle Root from on-chain mapping (GetBatchAsync)
            var batchResult = await _blockchainService.GetBatchAsync(batchId.Value.ToString(), ct);
            if (batchResult.IsFailure || !batchResult.Value.Exists)
            {
                _logger.LogWarning("On-chain Batch not found or failed query for BatchId={BatchId}. Error={Error}", batchId, batchResult.Error.Message);
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            string onChainMerkleRoot = batchResult.Value.MerkleRoot;

            // Verify Merkle Proof
            if (string.IsNullOrEmpty(snapshot.MerkleProofJson))
            {
                _logger.LogWarning("Merkle proof is missing in local snapshot for DegreeCode={DegreeCode}", request.DegreeCode);
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            MerkleProofData proofData;
            try
            {
                proofData = JsonSerializer.Deserialize<MerkleProofData>(snapshot.MerkleProofJson)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Merkle proof.");
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            bool isProofValid = _merkleTreeService.VerifyProof(snapshot.DataHash, proofData, onChainMerkleRoot);
            if (!isProofValid)
            {
                _logger.LogWarning("Merkle proof verification failed on-chain for DegreeCode={DegreeCode}", request.DegreeCode);
                await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            // 5. Success
            await LogVerificationAttemptAsync(snapshot.DegreeId, request.DegreeCode, snapshot.Version, VerificationResult.Verified, ct);
            return Result<VerifyDegreeResponse>.Success(new VerifyDegreeResponse(
                Verified: true,
                Status: snapshot.Status.ToString(),
                VerificationSource: VerificationSource.Blockchain_Merkle_Root,
                DegreeCode: request.DegreeCode,
                Version: snapshot.Version,
                InstitutionName: snapshot.InstitutionName,
                StudentFullName: snapshot.StudentFullName,
                Major: snapshot.Major,
                Classification: snapshot.Classification,
                IssuedAt: snapshot.IssuedAt,
                Blockchain: new BlockchainDetails(
                    TxHash: snapshot.TxHash,
                    BlockNumber: null,
                    MerkleRoot: onChainMerkleRoot,
                    MerkleProofJson: snapshot.MerkleProofJson
                )
            ));
        }

        private async Task LogVerificationAttemptAsync(Guid degreeId, string degreeCode, int? version, VerificationResult result, CancellationToken ct)
        {
            var logDetails = JsonSerializer.Serialize(new
            {
                DegreeCode = degreeCode,
                VersionVerified = version?.ToString() ?? "current",
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow
            });

            await _behaviorLogService.LogAsync(
                ActionTypeEnum.VERIFY_DEGREE,
                "DEGREES",
                targetId: degreeId,
                oldValuesJson: null,
                newValuesJson: logDetails,
                ct);
        }
    }
}
