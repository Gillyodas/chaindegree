using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class BlockchainStartupValidatorService : IHostedService
    {
        private readonly ILogger<BlockchainStartupValidatorService> _logger;
        private readonly BlockchainOptions _options;
        private readonly IBlockchainSigner _signer;

        public BlockchainStartupValidatorService(
            ILogger<BlockchainStartupValidatorService> logger,
            IOptions<BlockchainOptions> options,
            IBlockchainSigner signer)
        {
            _logger = logger;
            _options = options.Value;
            _signer = signer;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating Blockchain Configuration...");

            if (string.IsNullOrWhiteSpace(_options.RpcUrl))
            {
                throw new InvalidOperationException("Blockchain: RpcUrl is missing in configuration.");
            }

            var web3 = new Web3(_options.RpcUrl);

            // 1. Check ChainId
            _logger.LogInformation("Checking ChainId...");
            var chainId = await web3.Eth.ChainId.SendRequestAsync();
            if (chainId.Value != _options.ChainId)
            {
                throw new InvalidOperationException($"Blockchain: ChainId mismatch! Expected {_options.ChainId}, but network returned {chainId.Value}.");
            }

            // 2. Check Contract Code
            _logger.LogInformation("Checking Contract Code at {Address}...", _options.ContractAddress);
            var code = await web3.Eth.GetCode.SendRequestAsync(_options.ContractAddress);
            if (code == "0x")
            {
                throw new InvalidOperationException($"Blockchain: No contract found at address {_options.ContractAddress}. Did you forget to deploy?");
            }

            // 3. Check Signer Rights
            // We use the generalized approach: call authorizedAnchors(address) mapping.
            // Function ABI for `authorizedAnchors(address)` mapping is `mapping(address => bool)`.
            // Nethereum allows calling mapping using FunctionMessage or raw call.
            _logger.LogInformation("Checking Signer Authorization for address {Address}...", _signer.GetAddress());
            
            var contract = web3.Eth.GetContract(
                "[{\"inputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"name\":\"authorizedAnchors\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"}]",
                _options.ContractAddress);
                
            var authorizedAnchorsFunction = contract.GetFunction("authorizedAnchors");
            var isAuthorized = await authorizedAnchorsFunction.CallAsync<bool>(_signer.GetAddress());

            if (!isAuthorized)
            {
                throw new InvalidOperationException($"Blockchain: Signer {_signer.GetAddress()} is NOT authorized to anchor on contract {_options.ContractAddress}. Fail Fast.");
            }

            _logger.LogInformation("Blockchain Configuration Validation PASSED.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
