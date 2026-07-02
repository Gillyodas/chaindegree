using ChainDegree.SharedKernel.Common.Log;

namespace ChainDegree.SharedKernel.Common.Log
{
    public static class PersistenceLogs
    {
        public static readonly LogCode Persistence_TransactionDaBatDau = 
            new("Persistence.TransactionStarted", "Transaction has started.");

        public static readonly LogCode Persistence_ImplicitTransactionDaBatDau = 
            new("Persistence.ImplicitTransactionStarted", "Implicit transaction has started.");

        public static readonly LogCode Persistence_ImplicitTransactionDaCommit = 
            new("Persistence.ImplicitTransactionCommitted", "Implicit transaction committed successfully.");

        public static readonly LogCode Persistence_ImplicitTransactionThatBai = 
            new("Persistence.ImplicitTransactionFailed", "Implicit transaction failed.");

        public static readonly LogCode Persistence_ExplicitTransactionDaCommit = 
            new("Persistence.ExplicitTransactionCommitted", "Explicit transaction committed successfully.");

        public static readonly LogCode Persistence_ExplicitTransactionCommitThatBai = 
            new("Persistence.ExplicitTransactionCommitFailed", "Explicit transaction commit failed.");

        public static readonly LogCode Persistence_ExplicitTransactionDaRollback = 
            new("Persistence.ExplicitTransactionRolledBack", "Explicit transaction rolled back.");

        public static readonly LogCode Persistence_TransactionDaRollback = 
            new("Persistence.TransactionRolledBack", "Transaction rolled back.");

        public static readonly LogCode Persistence_RollbackThatBai = 
            new("Persistence.RollbackFailed", "Rollback failed.");

        public static readonly LogCode Persistence_XungDotDongThoi = 
            new("Persistence.ConcurrencyConflict", "A concurrency conflict occurred.");

        public static readonly LogCode Persistence_LoiCapNhatCSDL = 
            new("Persistence.DatabaseUpdateError", "A database update error occurred.");

        public static readonly LogCode Persistence_DangPhatDomainEvent = 
            new("Persistence.DispatchingDomainEvent", "Dispatching domain event.");
    }
}
