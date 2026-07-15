using ChainDegree.Core.Application.Abstractions.Blockchain;
using Microsoft.Extensions.Options;
using Nethereum.Web3.Accounts;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class LocalEnvSigner : IBlockchainSigner
    {
        private readonly Account _account;

        public LocalEnvSigner(IOptions<BlockchainOptions> options)
        {
            var pk = options.Value.PrivateKey;
            if (string.IsNullOrWhiteSpace(pk))
            {
                throw new System.ArgumentException("Blockchain PrivateKey is not configured.");
            }
            
            // Allow private keys with or without "0x" prefix
            if (pk.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase))
            {
                pk = pk.Substring(2);
            }
            
            _account = new Account(pk);
        }

        public string GetAddress()
        {
            return _account.Address;
        }
        
        public Account GetAccount()
        {
            return _account;
        }
    }
}
