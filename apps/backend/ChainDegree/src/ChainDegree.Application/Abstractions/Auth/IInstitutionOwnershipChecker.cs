using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Auth
{
    public interface IInstitutionOwnershipChecker
    {
        Task<bool> BelongsToInstitutionAsync(Guid userId, Guid institutionId, CancellationToken ct = default);
    }
}
