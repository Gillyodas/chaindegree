using System;
using System.Threading;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace ChainDegree.Core.Infrastructure.Blockchain
{
    public class NonceManager : INonceManager
    {
        private readonly IWeb3 _web3;
        private readonly IOptions<BlockchainOptions> _options;
        private readonly ILogger<NonceManager> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private long _nextNonce;
        private bool _isInitialized;

        public NonceManager(
            IWeb3 web3,
            IOptions<BlockchainOptions> options,
            ILogger<NonceManager> logger)
        {
            _web3 = web3;
            _options = options;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await InternalSyncNonceAsync(ct);
                _isInitialized = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<long> ReserveNonceAsync(CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                if (!_isInitialized)
                {
                    await InternalSyncNonceAsync(ct);
                    _isInitialized = true;
                }

                long reservedNonce = _nextNonce;
                _nextNonce++;
                _logger.LogDebug("Reserved Nonce {Nonce} for transaction.", reservedNonce);
                return reservedNonce;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ResyncAsync(CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await InternalSyncNonceAsync(ct);
                _isInitialized = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task InternalSyncNonceAsync(CancellationToken ct)
        {
            try
            {
                var address = _web3.TransactionManager?.Account?.Address;
                if (string.IsNullOrEmpty(address))
                {
                    _logger.LogWarning("Account address not found on IWeb3 TransactionManager. Unable to query transaction count.");
                    return;
                }

                var pendingNonceHex = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(
                    address,
                    BlockParameter.CreatePending());

                long pendingNonce = (long)pendingNonceHex.Value;
                _nextNonce = Math.Max(_nextNonce, pendingNonce);

                _logger.LogInformation("NonceManager synced for address {Address}. Pending Nonce on chain: {ChainNonce}, Local NextNonce: {LocalNonce}",
                    address, pendingNonce, _nextNonce);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query pending nonce count from blockchain RPC.");
                throw;
            }
        }
    }
}
