using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nethereum.Web3;

namespace ChainDegree.Core.Infrastructure.Monitoring
{
    public class BesuRpcHealthCheck : IHealthCheck
    {
        private readonly IWeb3 _web3;

        public BesuRpcHealthCheck(IWeb3 web3)
        {
            _web3 = web3;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                return HealthCheckResult.Healthy($"Besu RPC connected. Current block: {blockNumber.Value}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Besu RPC connection failed", ex);
            }
        }
    }
}
