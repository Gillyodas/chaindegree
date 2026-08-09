namespace ChainDegree.Core.Infrastructure.Persistence.Entities
{
    public static class DegreeProcessingState
    {
        public const string Queued = "Queued";
        public const string Processing = "Processing";
        public const string Unknown = "Unknown";
        public const string Submitted = "Submitted";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
