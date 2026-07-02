using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Auth;
using ChainDegree.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChainDegree.Core.Infrastructure.Auth
{
    public class FakeInstitutionOwnershipChecker : IInstitutionOwnershipChecker
    {
        private readonly ChainDegreeDbContext _context;

        public FakeInstitutionOwnershipChecker(ChainDegreeDbContext context)
        {
            _context = context;
        }

        public async Task<bool> BelongsToInstitutionAsync(Guid userId, Guid institutionId, CancellationToken ct = default)
        {
            if (userId == Guid.Parse("00000000-0000-0000-0000-000000000001"))
            {
                return true;
            }

            var belongs = await _context.Registrars
                .IgnoreQueryFilters()
                .AnyAsync(r => r.UserId == userId && r.InstitutionId == institutionId, ct);

            return belongs;
        }
    }
}
