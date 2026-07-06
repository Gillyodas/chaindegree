using System;

namespace ChainDegree.Core.Infrastructure.Configurations
{
    public class BesuOptions
    {
        public const string SectionName = "Blockchain:Besu";
        public string RpcUrl { get; set; } = "http://localhost:8545";
        public string AccountPrivateKey { get; set; } = string.Empty;
        public string ContractAddress { get; set; } = string.Empty;
        public int ChainId { get; set; } = 1337;
    }
}
