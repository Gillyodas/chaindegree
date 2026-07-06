using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Policies
{
    public interface IDegreeDuplicatePolicy
    {
        Task<bool> IsDuplicateAsync(
            Guid institutionId,
            Guid studentId,
            string major,
            int issuedYear,
            CancellationToken ct = default);
    }
}
