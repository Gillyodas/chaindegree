using ChainDegree.Core.Application.Abstractions.Blockchain;
using Nethereum.Web3;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class LocalEnvSigner : IBlockchainSigner
    {
        private readonly string _address;

        public LocalEnvSigner(IWeb3 web3)
        {
            _address = web3.TransactionManager.Account?.Address 
                       ?? throw new System.InvalidOperationException("IWeb3 instance has no configured account/signer.");
        }

        public string GetAddress()
        {
            return _address;
        }
    }
}
