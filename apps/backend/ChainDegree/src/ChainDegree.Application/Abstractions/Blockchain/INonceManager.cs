using System.Threading;
using System.Threading.Tasks;

namespace ChainDegree.Core.Application.Abstractions.Blockchain
{
    public interface INonceManager
    {
        /// <summary>
        /// Initializes the nonce manager by querying the pending transaction count from the blockchain node.
        /// </summary>
        Task InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// Atomically reserves and returns the next nonce in memory for transaction signing.
        /// </summary>
        Task<long> ReserveNonceAsync(CancellationToken ct = default);

        /// <summary>
        /// Resynchronizes local in-memory nonce state with the pending transaction count from the node.
        /// </summary>
        Task ResyncAsync(CancellationToken ct = default);
    }
}
