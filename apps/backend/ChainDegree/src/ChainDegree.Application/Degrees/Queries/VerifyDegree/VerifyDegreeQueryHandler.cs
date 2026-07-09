using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees.Enums;
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
        private readonly IBehaviorLogService _behaviorLogService;
        private readonly ILogger<VerifyDegreeQueryHandler> _logger;

        public VerifyDegreeQueryHandler(
            IDegreeRepository degreeRepository,
            IBlockchainService blockchainService,
            IMerkleTreeService merkleTreeService,
            IDegreeHashService degreeHashService,
            IBehaviorLogService behaviorLogService,
            ILogger<VerifyDegreeQueryHandler> logger)
        {
            _degreeRepository = degreeRepository;
            _blockchainService = blockchainService;
            _merkleTreeService = merkleTreeService;
            _degreeHashService = degreeHashService;
            _behaviorLogService = behaviorLogService;
            _logger = logger;
        }

        public async Task<Result<VerifyDegreeResponse>> Handle(VerifyDegreeQuery request, CancellationToken ct)
        {
            _logger.LogInformation("Verification request received for DegreeCode={DegreeCode}, Version={Version}",
                request.DegreeCode, request.Version);

            // 1. Resolve snapshot
            var snapshot = await _degreeRepository.GetVerificationSnapshotAsync(request.DegreeCode, request.Version, ct);
            if (snapshot == null)
            {
                if (request.Version.HasValue)
                {
                    await LogVerificationAttemptAsync(request.DegreeCode, request.Version, VerificationResult.UnsupportedVersion, ct);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.UnsupportedVersion);
                }
                else
                {
                    await LogVerificationAttemptAsync(request.DegreeCode, null, VerificationResult.DegreeNotFound, ct);
                    return Result<VerifyDegreeResponse>.Failure(DegreeErrors.NotFound);
                }
            }

            // Cross-check IssuedAt if provided
            if (request.IssuedAt.HasValue && snapshot.IssuedAt.Date != request.IssuedAt.Value.Date)
            {
                // If the dates don't match, we treat it as local data tampering (CryptoHashMismatch)
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
            }

            // 2. Status Check
            if (snapshot.Status == StatusEnum.Revoked)
            {
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.Revoked, ct);
                return Result<VerifyDegreeResponse>.Success(new VerifyDegreeResponse(
                    Verified: false,
                    Status: "Revoked",
                    DegreeCode: request.DegreeCode,
                    Version: snapshot.Version,
                    StudentFullName: snapshot.StudentFullName,
                    Major: snapshot.Major,
                    Classification: snapshot.Classification,
                    IssuedAt: snapshot.IssuedAt,
                    Blockchain: null
                ));
            }

            // 3. Local Integrity Check
            var originalIssuedAt = snapshot.IssuedAt;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(snapshot.PlainDataJson);
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

            string recalculatedHash;
            try
            {
                recalculatedHash = await _degreeHashService.CalculateHashAsync(degreeData, snapshot.Salt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate degree hash.");
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
            }

            if (!string.Equals(recalculatedHash, snapshot.DataHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Local integrity check failed for DegreeCode={DegreeCode}. Expected={Expected}, Computed={Computed}",
                    request.DegreeCode, snapshot.DataHash, recalculatedHash);
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.CryptoHashMismatch, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.CryptoHashMismatch);
            }

            // 4. Blockchain Integrity Check
            if (snapshot.Status != StatusEnum.Confirmed || string.IsNullOrEmpty(snapshot.TxHash))
            {
                // Not confirmed on chain yet
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            // Fetch Merkle Root from contract event log / state
            string? onChainMerkleRoot = await _blockchainService.GetAnchoredMerkleRootAsync(snapshot.TxHash, ct);
            if (string.IsNullOrEmpty(onChainMerkleRoot))
            {
                _logger.LogWarning("On-chain Merkle Root not found for TxHash={TxHash}", snapshot.TxHash);
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            // Verify Merkle Proof
            if (string.IsNullOrEmpty(snapshot.MerkleProofJson))
            {
                _logger.LogWarning("Merkle proof is missing in local snapshot for DegreeCode={DegreeCode}", request.DegreeCode);
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            MerkleProofData proofData;
            try
            {
                proofData = System.Text.Json.JsonSerializer.Deserialize<MerkleProofData>(snapshot.MerkleProofJson)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize Merkle proof.");
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            bool isProofValid = _merkleTreeService.VerifyProof(snapshot.DataHash, proofData, onChainMerkleRoot);
            if (!isProofValid)
            {
                _logger.LogWarning("Merkle proof verification failed on-chain for DegreeCode={DegreeCode}", request.DegreeCode);
                await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.BlockchainInvalid, ct);
                return Result<VerifyDegreeResponse>.Failure(DegreeErrors.BlockchainInvalid);
            }

            // 5. Success
            await LogVerificationAttemptAsync(request.DegreeCode, snapshot.Version, VerificationResult.Verified, ct);
            return Result<VerifyDegreeResponse>.Success(new VerifyDegreeResponse(
                Verified: true,
                Status: snapshot.Status.ToString(),
                DegreeCode: request.DegreeCode,
                Version: snapshot.Version,
                StudentFullName: snapshot.StudentFullName,
                Major: snapshot.Major,
                Classification: snapshot.Classification,
                IssuedAt: snapshot.IssuedAt,
                Blockchain: new BlockchainDetails(
                    TxHash: snapshot.TxHash,
                    BlockNumber: null, // BlockNumber can be added if available, but TxHash + MerkleRoot is sufficient
                    MerkleRoot: onChainMerkleRoot,
                    MerkleProofJson: snapshot.MerkleProofJson
                )
            ));
        }

        private async Task LogVerificationAttemptAsync(string degreeCode, int? version, VerificationResult result, CancellationToken ct)
        {
            var logDetails = System.Text.Json.JsonSerializer.Serialize(new
            {
                DegreeCode = degreeCode,
                VersionVerified = version?.ToString() ?? "current",
                Result = result.ToString(),
                Timestamp = DateTime.UtcNow
            });

            // Ghi nhận nhật ký hành vi thông qua BehaviorLogService với system actor ID mặc định (Anonymous)
            await _behaviorLogService.LogAsync(
                ActionTypeEnum.VERIFY_DEGREE,
                "DEGREES",
                targetId: Guid.Parse("00000000-0000-0000-0000-000000000002"), // Fixed sentinel ID for verification attempts
                oldValuesJson: null,
                newValuesJson: logDetails,
                ct);
        }
    }
}
