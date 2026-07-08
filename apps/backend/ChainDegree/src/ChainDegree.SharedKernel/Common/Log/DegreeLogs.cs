using ChainDegree.SharedKernel.Common.Log;

namespace ChainDegree.SharedKernel.Common.Log
{
    public static class DegreeLogs
    {
        public static readonly LogCode Degree_IssuanceInitiated =
            new("Degree.IssuanceInitiated", "Degree issuance process has started.");

        public static readonly LogCode Degree_IssuanceCompleted =
            new("Degree.IssuanceCompleted", "Degree issuance completed successfully.");

        public static readonly LogCode Degree_IssuancePartialSuccess =
            new("Degree.IssuancePartialSuccess", "Degree issuance completed with some failures.");

        public static readonly LogCode Degree_DuplicateDetected =
            new("Degree.DuplicateDetected", "Duplicate degree detected for student at institution.");

        public static readonly LogCode Degree_CryptoHashGenerated =
            new("Degree.CryptoHashGenerated", "Cryptographic hash generated for degree.");

        public static readonly LogCode Degree_BatchCreated =
            new("Degree.BatchCreated", "Batch tracking record created for degree issuance.");

        public static readonly LogCode Degree_BlockchainSyncStarted =
            new("Degree.BlockchainSyncStarted", "Blockchain sync process started for batch.");

        public static readonly LogCode Degree_BlockchainSyncCompleted =
            new("Degree.BlockchainSyncCompleted", "Blockchain sync completed successfully.");

        public static readonly LogCode Degree_BlockchainSyncFailed =
            new("Degree.BlockchainSyncFailed", "Blockchain sync failed for batch.");

        public static readonly LogCode Degree_RetryInitiated =
            new("Degree.RetryInitiated", "Retry initiated for degree with confirmation error.");
 
        public static readonly LogCode Degree_UpdateInitiated =
            new("Degree.UpdateInitiated", "Degree update process has started.");
 
        public static readonly LogCode Degree_UpdateCompleted =
            new("Degree.UpdateCompleted", "Degree update completed successfully.");
 
        public static readonly LogCode Degree_RevocationInitiated =
            new("Degree.RevocationInitiated", "Degree revocation process has started.");
 
        public static readonly LogCode Degree_RevocationCompleted =
            new("Degree.RevocationCompleted", "Degree revocation completed successfully.");
    }
}
