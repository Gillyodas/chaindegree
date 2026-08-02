namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class BlockchainOptions
    {
        public const string SectionName = "Blockchain";

        public string RpcUrl { get; set; } = string.Empty;
        public int ChainId { get; set; }
        public string ContractAddress { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public int ConfirmationCount { get; set; } = 1;
        public bool ValidateOnStartup { get; set; } = false;
    }
}
