using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.SharedKernel.Result;
using ChainDegree.SharedKernel.DomainErrors.Blockchain;
using DomainError = ChainDegree.SharedKernel.Common.Error.Error;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class NethereumBlockchainService : IBlockchainService
    {
        private readonly BlockchainOptions _options;
        private readonly ILogger<NethereumBlockchainService> _logger;
        private readonly IWeb3 _web3;

        public NethereumBlockchainService(
            IOptions<BlockchainOptions> options,
            IWeb3 web3,
            ILogger<NethereumBlockchainService> logger)
        {
            _options = options.Value;
            _web3 = web3;
            _logger = logger;
        }

        public async Task<Result<AnchorResult>> AnchorMerkleRootAsync(
            string batchId,
            string merkleRoot,
            string institutionId,
            string actionType,
            CancellationToken ct = default)
        {
            try
            {
                byte[] batchIdBytes = EnsureBytes32(batchId);
                byte[] merkleRootBytes = EnsureBytes32(merkleRoot);
                byte[] instIdBytes = EnsureBytes32(institutionId);

                var functionMessage = new AnchorMerkleRootFunction
                {
                    BatchId = batchIdBytes,
                    MerkleRoot = merkleRootBytes,
                    InstitutionId = instIdBytes,
                    ActionType = actionType,
                    GasPrice = new Nethereum.Hex.HexTypes.HexBigInteger(0),
                    Gas = new Nethereum.Hex.HexTypes.HexBigInteger(3000000)
                };

                var handler = _web3.Eth.GetContractTransactionHandler<AnchorMerkleRootFunction>();
                
                _logger.LogInformation("Sending AnchorMerkleRoot transaction for BatchId {BatchId}...", batchId);
                
                var txHash = await handler.SendRequestAsync(_options.ContractAddress, functionMessage);

                var anchorResult = new AnchorResult(
                    txHash,
                    null,
                    DateTimeOffset.UtcNow
                );

                return Result<AnchorResult>.Success(anchorResult);
            }
            catch (Exception ex) when (IsBlockchainException(ex))
            {
                return Result<AnchorResult>.Failure(MapExceptionToError(ex));
            }
        }

        public async Task<Result<BatchMetadata>> GetBatchAsync(string batchId, CancellationToken ct = default)
        {
            try
            {
                var handler = _web3.Eth.GetContractQueryHandler<BatchesFunction>();
                var function = new BatchesFunction { BatchId = EnsureBytes32(batchId) };
                
                var result = await handler.QueryDeserializingToObjectAsync<BatchesOutputDTO>(function, _options.ContractAddress);
                if (result == null)
                {
                    return Result<BatchMetadata>.Failure(BlockchainErrors.TransactionNotFound);
                }

                var metadata = new BatchMetadata(
                    "0x" + result.MerkleRoot.ToHex(),
                    (ulong)result.Timestamp,
                    "0x" + result.InstitutionId.ToHex(),
                    result.ActionType,
                    result.Exists
                );

                return Result<BatchMetadata>.Success(metadata);
            }
            catch (Exception ex) when (IsBlockchainException(ex))
            {
                return Result<BatchMetadata>.Failure(MapExceptionToError(ex));
            }
        }

        public async Task<Result<TransactionStatus>> GetTransactionStatusAsync(string txHash, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(txHash))
            {
                return Result<TransactionStatus>.Failure(BlockchainErrors.TransactionNotFound);
            }

            try
            {
                var transaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txHash);
                if (transaction == null)
                {
                    return Result<TransactionStatus>.Success(TransactionStatus.NotFound);
                }

                var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
                if (receipt == null)
                {
                    return Result<TransactionStatus>.Success(TransactionStatus.Pending);
                }
                
                if (receipt.Status.Value == 1)
                {
                    return Result<TransactionStatus>.Success(TransactionStatus.Confirmed);
                }
                
                return Result<TransactionStatus>.Success(TransactionStatus.Failed);
            }
            catch (Exception ex) when (IsBlockchainException(ex))
            {
                return Result<TransactionStatus>.Failure(MapExceptionToError(ex));
            }
        }

        private bool IsBlockchainException(Exception ex)
        {
            return ex is SmartContractRevertException
                || ex is TaskCanceledException
                || ex is System.Net.Http.HttpRequestException
                || ex is System.Net.Sockets.SocketException
                || ex.GetType().FullName?.Contains("Nethereum") == true;
        }

        private DomainError MapExceptionToError(Exception ex)
        {
            _logger.LogError(ex, "Blockchain interaction failed: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);

            var message = ex.Message.ToLowerInvariant();

            if (ex is SmartContractRevertException || message.Contains("revert"))
            {
                return BlockchainErrors.ContractReverted;
            }

            if (ex is TaskCanceledException || message.Contains("timeout"))
            {
                return BlockchainErrors.NetworkTimeout;
            }

            if (ex is System.Net.Http.HttpRequestException || message.Contains("connection refused") || message.Contains("failed to connect") || message.Contains("503"))
            {
                return BlockchainErrors.RpcUnavailable;
            }

            if (message.Contains("unauthorized") || message.Contains("not authorized") || message.Contains("sender"))
            {
                return BlockchainErrors.Unauthorized;
            }

            if (ex.GetType().FullName?.Contains("Rpc") == true)
            {
                return new DomainError("Blockchain.RpcError", ex.Message);
            }

            return new DomainError("Blockchain.UnexpectedError", ex.Message);
        }

        private byte[] EnsureBytes32(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }
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
}
