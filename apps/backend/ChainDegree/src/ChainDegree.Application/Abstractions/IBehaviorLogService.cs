using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.SharedKernel.Enums;

namespace ChainDegree.Core.Application.Abstractions
{
    public interface IBehaviorLogService
    {
        Task LogAsync(
            ActionTypeEnum actionType,
            string targetTable,
            Guid targetId,
            string? oldValuesJson,
            string newValuesJson,
            CancellationToken ct = default);
    }
}
