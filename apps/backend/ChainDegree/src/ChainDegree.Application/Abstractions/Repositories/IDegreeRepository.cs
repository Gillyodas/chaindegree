using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Domain.Degrees.Entities;
using ChainDegree.Core.Domain.Degrees.ValueObjects;
using ChainDegree.Core.Application.Degrees.Queries.ListDegreeVersions;

namespace ChainDegree.Core.Application.Abstractions.Repositories
{
    public interface IDegreeRepository
    {
        Task<Degree?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<bool> ExistsDuplicateAsync(Guid institutionId, Guid studentId, string major, CancellationToken ct = default);
        Task<bool> ExistsDuplicatePolicyAsync(Guid institutionId, Guid studentId, string major, int issuedYear, CancellationToken ct = default);
        Task<long> GetTotalCountAsync(CancellationToken ct = default);
        Task AddAsync(Degree degree, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<Degree> degrees, CancellationToken ct = default);
        Task<List<Degree>> GetPendingConfirmationAsync(int batchSize, CancellationToken ct = default);
        Task AddUpdateRequestAsync(DegreeUpdateRequest request, CancellationToken ct = default);
        Task<DegreeUpdateRequest?> GetUpdateRequestByDegreeIdAsync(Guid degreeId, CancellationToken ct = default);
        void RemoveUpdateRequest(DegreeUpdateRequest request);
        Task<VerificationSnapshot?> GetVerificationSnapshotAsync(string degreeCode, int? version, CancellationToken ct = default);
        Task<DegreeVersionListResponse?> GetDegreeVersionsAsync(string degreeCode, CancellationToken ct = default);
        Task<Guid?> GetBatchIdByDegreeIdAsync(Guid degreeId, int? version = null, CancellationToken ct = default);
    }
}
