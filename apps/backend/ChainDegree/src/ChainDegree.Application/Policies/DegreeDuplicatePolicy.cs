using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Policies;
using ChainDegree.Core.Application.Abstractions.Repositories;

namespace ChainDegree.Core.Application.Policies
{
    public class DegreeDuplicatePolicy : IDegreeDuplicatePolicy
    {
        private readonly IDegreeRepository _degreeRepository;

        public DegreeDuplicatePolicy(IDegreeRepository degreeRepository)
        {
            _degreeRepository = degreeRepository;
        }

        public async Task<bool> IsDuplicateAsync(
            Guid institutionId,
            Guid studentId,
            string major,
            int issuedYear,
            CancellationToken ct = default)
        {
            return await _degreeRepository.ExistsDuplicatePolicyAsync(institutionId, studentId, major, issuedYear, ct);
        }
    }
}
