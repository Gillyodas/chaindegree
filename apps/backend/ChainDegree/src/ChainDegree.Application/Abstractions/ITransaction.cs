using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions
{
    public interface ITransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
    }
}
