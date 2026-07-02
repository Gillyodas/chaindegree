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
                throw new InvalidOperationException("Transaction has already started. Nested transactions are not supported."); 
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(ct);

            _logger.LogInformation("{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                PersistenceLogs.Persistence_TransactionStarted,
                PersistenceLogs.Persistence_TransactionStarted.Message,
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
                        PersistenceLogs.Persistence_ImplicitTransactionStarted,
                        PersistenceLogs.Persistence_ImplicitTransactionStarted.Message,
                        transaction.TransactionId);
                    
                    changes = await SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    
                    _logger.LogInformation(
                        "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId} | Changes: {Changes}",
                        PersistenceLogs.Persistence_ImplicitTransactionCommitted,
                        PersistenceLogs.Persistence_ImplicitTransactionCommitted.Message,
                        transaction.TransactionId,
                        changes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                        PersistenceLogs.Persistence_ImplicitTransactionFailed,
                        PersistenceLogs.Persistence_ImplicitTransactionFailed.Message,
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
                    PersistenceLogs.Persistence_ExplicitTransactionCommitted,
                    PersistenceLogs.Persistence_ExplicitTransactionCommitted.Message,
                    _currentTransaction.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                    PersistenceLogs.Persistence_ExplicitTransactionCommitFailed,
                    PersistenceLogs.Persistence_ExplicitTransactionCommitFailed.Message,
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
                    PersistenceLogs.Persistence_ExplicitTransactionRolledBack,
                    PersistenceLogs.Persistence_ExplicitTransactionRolledBack.Message,
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
                    PersistenceLogs.Persistence_ConcurrencyConflict,
                    PersistenceLogs.Persistence_ConcurrencyConflict.Message);
                throw new RepositoryConcurrencyException(
                    "A concurrency conflict occurred.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "{@LogCode} | Message: {@LogMessage}",
                    PersistenceLogs.Persistence_DatabaseUpdateError,
                    PersistenceLogs.Persistence_DatabaseUpdateError.Message);
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
                    PersistenceLogs.Persistence_TransactionRolledBack,
                    PersistenceLogs.Persistence_TransactionRolledBack.Message,
                    transaction.TransactionId);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(rollbackEx,
                    "{@LogCode} | Message: {@LogMessage} | TransactionId: {TransactionId}",
                    PersistenceLogs.Persistence_RollbackFailed,
                    PersistenceLogs.Persistence_RollbackFailed.Message,
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
