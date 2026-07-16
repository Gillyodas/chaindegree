using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class BlockchainStartupValidatorService : IHostedService
    {
        private readonly ILogger<BlockchainStartupValidatorService> _logger;
        private readonly BlockchainOptions _options;
        private readonly IWeb3 _web3;
        private readonly IBlockchainSigner _signer;

        public BlockchainStartupValidatorService(
            ILogger<BlockchainStartupValidatorService> logger,
            IOptions<BlockchainOptions> options,
            IWeb3 web3,
            IBlockchainSigner signer)
        {
            _logger = logger;
            _options = options.Value;
            _web3 = web3;
            _signer = signer;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating Blockchain Configuration...");

            if (string.IsNullOrWhiteSpace(_options.RpcUrl))
            {
                throw new InvalidOperationException("Blockchain: RpcUrl is missing in configuration.");
            }

            // 1. Check ChainId
            _logger.LogInformation("Checking ChainId...");
            var chainId = await _web3.Eth.ChainId.SendRequestAsync();
            if (chainId.Value != _options.ChainId)
            {
                throw new InvalidOperationException($"Blockchain: ChainId mismatch! Expected {_options.ChainId}, but network returned {chainId.Value}.");
            }

            // 2. Check Contract Code
            _logger.LogInformation("Checking Contract Code at {Address}...", _options.ContractAddress);
            var code = await _web3.Eth.GetCode.SendRequestAsync(_options.ContractAddress);
            if (code == "0x")
            {
                throw new InvalidOperationException($"Blockchain: No contract found at address {_options.ContractAddress}. Did you forget to deploy?");
            }

            // 3. Check Signer Rights using strongly-typed query
            var signerAddress = _signer.GetAddress();
            _logger.LogInformation("Checking Signer Authorization for address {Address}...", signerAddress);
            
            var handler = _web3.Eth.GetContractQueryHandler<AuthorizedAnchorsFunction>();
            var function = new AuthorizedAnchorsFunction { SignerAddress = signerAddress };
            
            var isAuthorized = await handler.QueryAsync<bool>(_options.ContractAddress, function);

            if (!isAuthorized)
            {
                throw new InvalidOperationException($"Blockchain: Signer {signerAddress} is NOT authorized to anchor on contract {_options.ContractAddress}. Fail Fast.");
            }

            _logger.LogInformation("Blockchain Configuration Validation PASSED.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Function("authorizedAnchors", "bool")]
    public class AuthorizedAnchorsFunction : FunctionMessage
    {
        [Parameter("address", "", 1)]
        public string SignerAddress { get; set; } = null!;
    }
}
