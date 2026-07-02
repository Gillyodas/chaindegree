using ChainDegree.SharedKernel.Common.Log;

namespace ChainDegree.SharedKernel.Common.Log
{
    public static class PersistenceLogs
    {
        public static readonly LogCode Persistence_TransactionStarted = 
            new("Persistence.TransactionStarted", "Transaction has started.");

        public static readonly LogCode Persistence_ImplicitTransactionStarted = 
            new("Persistence.ImplicitTransactionStarted", "Implicit transaction has started.");

        public static readonly LogCode Persistence_ImplicitTransactionCommitted = 
            new("Persistence.ImplicitTransactionCommitted", "Implicit transaction committed successfully.");

        public static readonly LogCode Persistence_ImplicitTransactionFailed = 
            new("Persistence.ImplicitTransactionFailed", "Implicit transaction failed.");

        public static readonly LogCode Persistence_ExplicitTransactionCommitted = 
            new("Persistence.ExplicitTransactionCommitted", "Explicit transaction committed successfully.");

        public static readonly LogCode Persistence_ExplicitTransactionCommitFailed = 
            new("Persistence.ExplicitTransactionCommitFailed", "Explicit transaction commit failed.");

        public static readonly LogCode Persistence_ExplicitTransactionRolledBack = 
            new("Persistence.ExplicitTransactionRolledBack", "Explicit transaction rolled back.");

        public static readonly LogCode Persistence_TransactionRolledBack = 
            new("Persistence.TransactionRolledBack", "Transaction rolled back.");

        public static readonly LogCode Persistence_RollbackFailed = 
            new("Persistence.RollbackFailed", "Rollback failed.");

        public static readonly LogCode Persistence_ConcurrencyConflict = 
            new("Persistence.ConcurrencyConflict", "A concurrency conflict occurred.");

        public static readonly LogCode Persistence_DatabaseUpdateError = 
            new("Persistence.DatabaseUpdateError", "A database update error occurred.");

        public static readonly LogCode Persistence_DispatchingDomainEvent = 
            new("Persistence.DispatchingDomainEvent", "Dispatching domain event.");
    }
}
