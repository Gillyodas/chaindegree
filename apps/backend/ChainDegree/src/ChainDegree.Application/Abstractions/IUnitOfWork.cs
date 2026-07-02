using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        bool HasActiveTransaction { get; }
        Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
        Task<int> CommitAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);
        Task RollbackTransactionAsync(CancellationToken ct = default);
    }
}
