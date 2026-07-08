using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Domain.Degrees.ValueObjects;

namespace ChainDegree.Core.Application.Abstractions.Crypto
{
    public record DegreeData(
        string DegreeCode,
        Guid StudentId,
        string Major,
        string Classification,
        DateTime IssuedAt);

    public interface IDegreeHashService
    {
        Task<CryptoSnapshot> RecalculateAsync(DegreeData data, CancellationToken ct = default);
    }
}
