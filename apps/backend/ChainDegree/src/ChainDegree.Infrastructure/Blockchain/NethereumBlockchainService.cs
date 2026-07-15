using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class NethereumBlockchainService : IBlockchainService
    {
        private readonly BlockchainOptions _options;
        private readonly IBlockchainSigner _signer;
        private readonly ILogger<NethereumBlockchainService> _logger;
        private readonly Web3 _web3;

        public NethereumBlockchainService(
            IOptions<BlockchainOptions> options,
            IBlockchainSigner signer,
            ILogger<NethereumBlockchainService> logger)
        {
            _options = options.Value;
            _signer = signer;
            _logger = logger;
            
            var account = ((LocalEnvSigner)_signer).GetAccount();
            _web3 = new Web3(account, _options.RpcUrl);
            _web3.TransactionManager.UseLegacyAsDefault = true; // For local besu without EIP1559 if needed, but modern Besu supports it. We'll leave it default.
        }

        public async Task<AnchorResult> AnchorMerkleRootAsync(
            string batchId,
            string merkleRoot,
            string institutionId,
            string actionType,
            CancellationToken ct = default)
        {
            byte[] batchIdBytes = EnsureBytes32(batchId);
            byte[] merkleRootBytes = EnsureBytes32(merkleRoot);
            byte[] instIdBytes = EnsureBytes32(institutionId);

            var functionMessage = new AnchorMerkleRootFunction
            {
                BatchId = batchIdBytes,
                MerkleRoot = merkleRootBytes,
                InstitutionId = instIdBytes,
                ActionType = actionType
            };

            var handler = _web3.Eth.GetContractTransactionHandler<AnchorMerkleRootFunction>();
            
            _logger.LogInformation("Sending AnchorMerkleRoot transaction for BatchId {BatchId}...", batchId);
            
            // Note: SendRequestAsync returns TxHash immediately.
            var txHash = await handler.SendRequestAsync(_options.ContractAddress, functionMessage);

            return new AnchorResult
            {
                TransactionHash = txHash,
                BlockNumber = null,
                SubmittedAt = DateTimeOffset.UtcNow
            };
        }

        public async Task<bool> CheckBatchExistsAsync(string batchId, CancellationToken ct = default)
        {
            var handler = _web3.Eth.GetContractQueryHandler<BatchesFunction>();
            var function = new BatchesFunction { BatchId = EnsureBytes32(batchId) };
            
            var result = await handler.QueryDeserializingToObjectAsync<BatchesOutputDTO>(function, _options.ContractAddress);
            return result != null && result.Exists;
        }

        public async Task<TransactionStatus> GetTransactionStatusAsync(string txHash, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(txHash))
            {
                return TransactionStatus.NotFound;
            }

            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            if (receipt == null)
            {
                return TransactionStatus.NotFound;
            }
            
            if (receipt.Status.Value == 1)
            {
                return TransactionStatus.Confirmed;
            }
            
            return TransactionStatus.Failed;
        }

        public async Task<string?> GetAnchoredMerkleRootAsync(string txHash, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(txHash)) return null;

            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            if (receipt == null || receipt.Status.Value != 1) return null;

            var eventLogs = receipt.DecodeAllEvents<BatchAnchoredEventDTO>();
            if (eventLogs.Count > 0)
            {
                return "0x" + eventLogs[0].Event.MerkleRoot.ToHex();
            }

            return null;
        }

        private byte[] EnsureBytes32(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }
            // Pad to 64 hex chars (32 bytes) if needed, though mostly it should be 32 bytes from Keccak256
            if (hex.Length < 64)
            {
                hex = hex.PadLeft(64, '0');
            }
            return hex.HexToByteArray();
        }
    }

    [Function("anchorMerkleRoot")]
    public class AnchorMerkleRootFunction : FunctionMessage
    {
        [Parameter("bytes32", "batchId", 1)]
        public byte[] BatchId { get; set; } = null!;

        [Parameter("bytes32", "merkleRoot", 2)]
        public byte[] MerkleRoot { get; set; } = null!;

        [Parameter("bytes32", "institutionId", 3)]
        public byte[] InstitutionId { get; set; } = null!;

        [Parameter("string", "actionType", 4)]
        public string ActionType { get; set; } = null!;
    }

    [Function("batches", "tuple")]
    public class BatchesFunction : FunctionMessage
    {
        [Parameter("bytes32", "", 1)]
        public byte[] BatchId { get; set; } = null!;
    }

    [FunctionOutput]
    public class BatchesOutputDTO : IFunctionOutputDTO
    {
        [Parameter("bytes32", "MerkleRoot", 1)]
        public byte[] MerkleRoot { get; set; } = null!;

        [Parameter("uint256", "Timestamp", 2)]
        public BigInteger Timestamp { get; set; }

        [Parameter("bytes32", "InstitutionId", 3)]
        public byte[] InstitutionId { get; set; } = null!;

        [Parameter("string", "ActionType", 4)]
        public string ActionType { get; set; } = null!;

        [Parameter("bool", "Exists", 5)]
        public bool Exists { get; set; }
    }

    [Event("BatchAnchored")]
    public class BatchAnchoredEventDTO : IEventDTO
    {
        [Parameter("bytes32", "batchId", 1, true)]
        public byte[] BatchId { get; set; } = null!;

        [Parameter("bytes32", "merkleRoot", 2, false)]
        public byte[] MerkleRoot { get; set; } = null!;

        [Parameter("uint256", "timestamp", 3, false)]
        public BigInteger Timestamp { get; set; }
    }
}
