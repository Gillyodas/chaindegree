using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Repositories;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees.Enums;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Domain.Students;
using ChainDegree.Core.Infrastructure.Persistence.Locking;
using ChainDegree.Core.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Persistence.Repositories
{
    public class DegreeRepository : IDegreeRepository
    {
        private readonly ChainDegreeDbContext _context;
        private readonly IPendingDegreeLockStrategy _lockStrategy;

        public DegreeRepository(ChainDegreeDbContext context, IPendingDegreeLockStrategy lockStrategy)
        {
            _context = context;
            _lockStrategy = lockStrategy;
        }

        public async Task<Degree?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.Degrees.FirstOrDefaultAsync(x => x.Id == id, ct);
        }

        public async Task<bool> ExistsDuplicateAsync(Guid institutionId, Guid studentId, string major, CancellationToken ct = default)
        {
            return await _context.Degrees.AnyAsync(x =>
                x.InstitutionId == institutionId &&
                x.StudentId == studentId &&
                x.Major == major &&
                x.DeletedAt == null, ct);
        }

        public async Task<bool> ExistsDuplicatePolicyAsync(Guid institutionId, Guid studentId, string major, int issuedYear, CancellationToken ct = default)
        {
            return await _context.Degrees.AnyAsync(x =>
                x.InstitutionId == institutionId &&
                x.StudentId == studentId &&
                x.Major == major &&
                x.IssuedAt.Year == issuedYear &&
                x.DeletedAt == null, ct);
        }

        public async Task<long> GetTotalCountAsync(CancellationToken ct = default)
        {
            return await _context.Degrees.LongCountAsync(ct);
        }

        public async Task AddAsync(Degree degree, CancellationToken ct = default)
        {
            await _context.Degrees.AddAsync(degree, ct);
        }

        public async Task AddRangeAsync(IEnumerable<Degree> degrees, CancellationToken ct = default)
        {
            await _context.Degrees.AddRangeAsync(degrees, ct);
        }

        public async Task<List<Degree>> GetPendingConfirmationAsync(int batchSize, CancellationToken ct = default)
        {
            return await _lockStrategy.GetAndLockPendingDegreesAsync(_context, batchSize, ct);
        }

        public async Task AddUpdateRequestAsync(DegreeUpdateRequest request, CancellationToken ct = default)
        {
            await _context.DegreeUpdateRequests.AddAsync(request, ct);
        }

        public async Task<DegreeUpdateRequest?> GetUpdateRequestByDegreeIdAsync(Guid degreeId, CancellationToken ct = default)
        {
            return await _context.DegreeUpdateRequests.FirstOrDefaultAsync(x => x.DegreeId == degreeId, ct);
        }

        public void RemoveUpdateRequest(DegreeUpdateRequest request)
        {
            _context.DegreeUpdateRequests.Remove(request);
        }

        public async Task<VerificationSnapshot?> GetVerificationSnapshotAsync(string degreeCode, int? version, CancellationToken ct = default)
        {
            var degree = await _context.Degrees.FirstOrDefaultAsync(x => x.DegreeCode == degreeCode, ct);
            if (degree == null)
            {
                return null;
            }

            var student = await _context.Students.FirstOrDefaultAsync(x => x.Id == degree.StudentId, ct);
            var studentFullName = student?.FullName ?? "Unknown Student";

            if (version.HasValue && version.Value < degree.CurrentVersion)
            {
                var historicalVersion = await _context.DegreeVersions
                    .FirstOrDefaultAsync(x => x.DegreeId == degree.Id && x.Version == version.Value, ct);

                if (historicalVersion == null)
                {
                    return null;
                }

                return new VerificationSnapshot(
                    degreeId: historicalVersion.DegreeId,
                    dataHash: historicalVersion.CurrentHash,
                    salt: historicalVersion.Salt,
                    plainDataJson: historicalVersion.PlainDataJson,
                    txHash: historicalVersion.BlockchainTxHash,
                    merkleProofJson: historicalVersion.MerkleProofJson,
                    version: historicalVersion.Version,
                    status: degree.Status, // Status changes apply globally
                    studentFullName: studentFullName,
                    major: historicalVersion.Major,
                    classification: historicalVersion.Classification,
                    studentId: degree.StudentId,
                    issuedAt: degree.IssuedAt
                );
            }

            var batchDegreeRecord = await _context.BatchDegreeRecords
                .FirstOrDefaultAsync(x => x.DegreeId == degree.Id, ct);

            return new VerificationSnapshot(
                degreeId: degree.Id,
                dataHash: degree.CryptoData.DataHashLocal,
                salt: degree.CryptoData.Salt,
                plainDataJson: degree.CryptoData.PlainDataJson,
                txHash: degree.TxHashBlockchain ?? string.Empty,
                merkleProofJson: batchDegreeRecord != null 
                    ? ConstructMerkleProofJson(batchDegreeRecord, degree.CryptoData.DataHashLocal) 
                    : null,
                version: degree.CurrentVersion,
                status: degree.Status,
                studentFullName: studentFullName,
                major: degree.Major,
                classification: degree.Classification,
                studentId: degree.StudentId,
                issuedAt: degree.IssuedAt
            );
        }

        private string? ConstructMerkleProofJson(BatchDegreeRecord batchRecord, string leafHash)
        {
            if (string.IsNullOrEmpty(batchRecord.ProofHashesJson)) return null;

            try
            {
                var hashes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(batchRecord.ProofHashesJson);
                if (hashes == null) return null;

                var directions = new List<bool>();
                int currentIndex = batchRecord.LeafIndex;
                for (int i = 0; i < hashes.Count; i++)
                {
                    bool isSiblingRight = currentIndex % 2 == 0;
                    directions.Add(isSiblingRight);
                    currentIndex /= 2;
                }

                var proofData = new
                {
                    LeafIndex = batchRecord.LeafIndex,
                    LeafHash = leafHash,
                    ProofHashes = hashes,
                    ProofDirections = directions
                };

                return System.Text.Json.JsonSerializer.Serialize(proofData);
            }
            catch
            {
                return null;
            }
        }
        public async Task<Guid?> GetBatchIdByDegreeIdAsync(Guid degreeId, CancellationToken ct = default)
        {
            var record = await _context.BatchDegreeRecords
                .FirstOrDefaultAsync(x => x.DegreeId == degreeId, ct);
            return record?.BatchId;
        }
    }
}
