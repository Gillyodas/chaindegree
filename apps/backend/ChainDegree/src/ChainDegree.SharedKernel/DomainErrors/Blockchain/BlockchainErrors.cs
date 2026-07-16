using ChainDegree.SharedKernel.Result;

namespace ChainDegree.SharedKernel.DomainErrors.Blockchain
{
    public static class BlockchainErrors
    {
        public static readonly Error RpcUnavailable = new(
            "Blockchain.RpcUnavailable",
            "The blockchain RPC node is currently unavailable.");

        public static readonly Error NetworkTimeout = new(
            "Blockchain.NetworkTimeout",
            "A network timeout occurred while interacting with the blockchain.");

        public static readonly Error Unauthorized = new(
            "Blockchain.Unauthorized",
            "The signer is not authorized to perform this operation.");

        public static readonly Error ContractReverted = new(
            "Blockchain.ContractReverted",
            "The smart contract execution reverted.");

        public static readonly Error TransactionNotFound = new(
            "Blockchain.TransactionNotFound",
            "The transaction could not be found on the blockchain.");

        public static readonly Error InvalidChain = new(
            "Blockchain.InvalidChain",
            "The blockchain network returned an invalid or unexpected ChainId.");
    }
}
