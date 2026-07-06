using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Degrees;
using ChainDegree.Core.Application.Degrees.Commands.IssueDegree;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Application.Abstractions.Services
{
    public interface IDegreeIssuanceService
    {
        Task<PartialResult<Degree, IssueDegreeFailureDto>> IssueDegreesAsync(
            Guid institutionId,
            Guid registrarId,
            IReadOnlyList<IssueDegreeItemDto> items,
            CancellationToken ct = default);
    }
}
