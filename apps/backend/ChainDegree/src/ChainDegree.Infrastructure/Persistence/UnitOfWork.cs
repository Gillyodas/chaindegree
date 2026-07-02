using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions;
using ChainDegree.Core.Application.Common.Exceptions;
using ChainDegree.SharedKernel.Common.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ChainDegree.Core.Infrastructure.Persistence
{
    public class DbContextTransactionAdapter : ITransaction
    {
        private readonly IDbContextTransaction _transaction;

        public DbContextTransactionAdapter(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken ct = default) => _transaction.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct = default) => _transaction.RollbackAsync(ct);
        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ChainDegreeDbContext _dbContext;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(ChainDegreeDbContext dbContext, ILogger<UnitOfWork> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public bool HasActiveTransaction => _currentTransaction != null;

        public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("Transaction đã bắt đầu hoạt động. Không hỗ trợ transaction lồng nhau"); 
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(ct);

            _logger.LogInformation("{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                PersistenceLogs.Persistence_TransactionDaBatDau,
                PersistenceLogs.Persistence_TransactionDaBatDau.Message,
                _currentTransaction.TransactionId);

            return new DbContextTransactionAdapter(_currentTransaction);
        }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            if (_currentTransaction != null)
            {
                return await SaveChangesAsync(ct);
            }

            int changes;
            await using (var transaction = await _dbContext.Database.BeginTransactionAsync(ct))
            {
                try
                {
                    _logger.LogInformation("{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                        PersistenceLogs.Persistence_ImplicitTransactionDaBatDau,
                        PersistenceLogs.Persistence_ImplicitTransactionDaBatDau.Message,
                        transaction.TransactionId);
                    
                    changes = await SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    
                    _logger.LogInformation(
                        "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId} | Changes: {Changes}",
                        PersistenceLogs.Persistence_ImplicitTransactionDaCommit,
                        PersistenceLogs.Persistence_ImplicitTransactionDaCommit.Message,
                        transaction.TransactionId,
                        changes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                        PersistenceLogs.Persistence_ImplicitTransactionThatBai,
                        PersistenceLogs.Persistence_ImplicitTransactionThatBai.Message,
                        transaction.TransactionId);
                    await SafeRollbackAsync(transaction, ct);
                    _dbContext.ChangeTracker.Clear();
                    throw MapException(ex);
                }
            }

            return changes;
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            try
            {
                await _currentTransaction.CommitAsync(ct);
                _logger.LogInformation(
                    "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                    PersistenceLogs.Persistence_ExplicitTransactionDaCommit,
                    PersistenceLogs.Persistence_ExplicitTransactionDaCommit.Message,
                    _currentTransaction.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                    PersistenceLogs.Persistence_ExplicitTransactionCommitThatBai,
                    PersistenceLogs.Persistence_ExplicitTransactionCommitThatBai.Message,
                    _currentTransaction.TransactionId);
                await SafeRollbackAsync(_currentTransaction, ct);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction == null) return;

            try
            {
                await _currentTransaction.RollbackAsync(ct);
                _logger.LogWarning(
                    "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                    PersistenceLogs.Persistence_ExplicitTransactionDaRollback,
                    PersistenceLogs.Persistence_ExplicitTransactionDaRollback.Message,
                    _currentTransaction.TransactionId);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
                _dbContext.ChangeTracker.Clear();
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex,
                    "{@LogCode} | Message: {@LogMessage}",
                    PersistenceLogs.Persistence_XungDotDongThoi,
                    PersistenceLogs.Persistence_XungDotDongThoi.Message);
                throw new RepositoryConcurrencyException(
                    "A concurrency conflict occurred.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "{@LogCode} | Message: {@LogMessage}",
                    PersistenceLogs.Persistence_LoiCapNhatCSDL,
                    PersistenceLogs.Persistence_LoiCapNhatCSDL.Message);
                throw new RepositoryException(
                    "A database update error occurred.", ex);
            }
        }

        private async Task SafeRollbackAsync(IDbContextTransaction transaction, CancellationToken ct)
        {
            try
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(
                    "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                    PersistenceLogs.Persistence_TransactionDaRollback,
                    PersistenceLogs.Persistence_TransactionDaRollback.Message,
                    transaction.TransactionId);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(rollbackEx,
                    "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                    PersistenceLogs.Persistence_RollbackThatBai,
                    PersistenceLogs.Persistence_RollbackThatBai.Message,
                    transaction.TransactionId);
            }
        }

        private Exception MapException(Exception ex)
        {
            return ex switch
            {
                DbUpdateConcurrencyException concurrencyEx =>
                    new RepositoryConcurrencyException(
                        "A concurrency conflict occurred.", concurrencyEx),

                DbUpdateException dbEx =>
                    new RepositoryException(
                        "A database update error occurred.", dbEx),

                OperationCanceledException cancelEx =>
                    new RepositoryException(
                        "Transaction was cancelled.", cancelEx),

                _ => new RepositoryException(
                    "An unexpected error occurred during transaction.", ex)
            };
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
}
