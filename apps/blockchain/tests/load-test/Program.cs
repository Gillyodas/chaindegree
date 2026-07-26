using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChainDegree.Core.Application.Abstractions.Blockchain;
using ChainDegree.Core.Application.Abstractions.Crypto;
using ChainDegree.Core.Domain.Degrees.Interfaces;
using ChainDegree.Core.Infrastructure.Blockchain;
using ChainDegree.Core.Infrastructure.Configurations;
using ChainDegree.Core.Infrastructure.Cryptography.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace ChainDegree.LoadTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine(" ChainDegree Blockchain Pipeline Load Test Tool");
            Console.WriteLine("=================================================");

            var scenario = args.Length > 0 ? args[0].ToUpperInvariant() : "ALL";
            var enableOnChain = args.Any(a => a.Equals("--on-chain", StringComparison.OrdinalIgnoreCase) || a.Equals("-c", StringComparison.OrdinalIgnoreCase));

            var hashService = new Sha256HashService();
            var merkleService = new MerkleTreeService();
            var canonicalizer = new JsonCanonicalizer();
            IBlockchainService? blockchainService = enableOnChain ? CreateBlockchainService() : null;

            Console.WriteLine($"\nRunning Load Test Scenario: {scenario}");
            Console.WriteLine($"Mode: {(enableOnChain ? "ON-CHAIN ANCHORING (Besu Live Network)" : "CPU PIPELINE (Hashing & Merkle Build Only)")}\n");

            if (scenario == "LT-1" || scenario == "ALL")
            {
                await RunBenchmarkScenarioAsync("LT-1 (Light - 500 degrees)", 500, hashService, merkleService, canonicalizer, blockchainService, enableOnChain);
            }

            if (scenario == "LT-2" || scenario == "ALL")
            {
                await RunBenchmarkScenarioAsync("LT-2 (Medium - 1,000 degrees)", 1000, hashService, merkleService, canonicalizer, blockchainService, enableOnChain);
            }

            if (scenario == "LT-3" || scenario == "ALL")
            {
                await RunBenchmarkScenarioAsync("LT-3 (Heavy - 5,000 degrees)", 5000, hashService, merkleService, canonicalizer, blockchainService, enableOnChain);
            }

            if (scenario == "LT-4" || scenario == "ALL")
            {
                await RunBurstBenchmarkScenarioAsync("LT-4 (Burst - 500 degrees/sec)", 500, 10, hashService, merkleService, canonicalizer, blockchainService, enableOnChain);
            }

            Console.WriteLine("\n=================================================");
            Console.WriteLine(" All Load Test Scenarios Completed Successfully!");
            Console.WriteLine("=================================================");
        }

        private static IBlockchainService? CreateBlockchainService()
        {
            try
            {
                var rpcUrl = Environment.GetEnvironmentVariable("Blockchain__RpcUrl") ?? "http://localhost:8545";
                var contractAddress = Environment.GetEnvironmentVariable("Blockchain__ContractAddress") ?? "0x9a3DBCa554e9f6b9257aAa24010DA8377C57c17e";
                var privateKey = Environment.GetEnvironmentVariable("Blockchain__PrivateKey") ?? "0x8f2a55949038a9610f50fb23b5883af3b4ecb3c3bb792cbcefbd1542c692be63";

                var account = new Account(privateKey, 2026);
                var web3 = new Web3(account, rpcUrl);

                var options = Options.Create(new BlockchainOptions
                {
                    RpcUrl = rpcUrl,
                    ContractAddress = contractAddress,
                    PrivateKey = privateKey,
                    ChainId = 2026,
                    ConfirmationCount = 1
                });

                return new NethereumBlockchainService(options, web3, NullLogger<NethereumBlockchainService>.Instance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Could not initialize Blockchain service: {ex.Message}");
                return null;
            }
        }

        private static async Task RunBenchmarkScenarioAsync(
            string name, 
            int count, 
            IHashService hashService, 
            IMerkleTreeService merkleService,
            IJsonCanonicalizer canonicalizer,
            IBlockchainService? blockchainService,
            bool enableOnChain)
        {
            Console.WriteLine($"--- Starting Scenario: {name} ---");
            var processSw = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(true);

            // 1. Generate PlainData & Hashes
            var hashes = new List<string>(count);
            var hashSw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var plainData = new Dictionary<string, object>
                {
                    ["studentId"] = $"STD_{i:D6}",
                    ["degreeCode"] = $"DEG_{i:D6}",
                    ["major"] = "Computer Science",
                    ["classification"] = "Excellent",
                    ["issueDate"] = "2026-07-22"
                };

                var canonicalJson = canonicalizer.Canonicalize(plainData).Value;
                var salt = Guid.NewGuid().ToString("N");
                var hashResult = hashService.HashData(canonicalJson, salt);
                hashes.Add(hashResult.Value);
            }
            hashSw.Stop();

            // 2. Build Merkle Trees in batches of 500
            int batchSize = 500;
            int numBatches = (int)Math.Ceiling((double)count / batchSize);
            var merkleSw = Stopwatch.StartNew();

            var merkleRoots = new List<string>();
            for (int b = 0; b < numBatches; b++)
            {
                var batchHashes = hashes.Skip(b * batchSize).Take(batchSize).ToList();
                var treeResult = merkleService.BuildTree(batchHashes);
                merkleRoots.Add(treeResult.MerkleRoot);
            }
            merkleSw.Stop();

            // 3. OPTIONAL: Send Merkle Roots On-Chain to Besu Smart Contract
            long onChainTxDurationMs = 0;
            int successfulOnChainTxs = 0;

            if (enableOnChain && blockchainService != null)
            {
                var txSw = Stopwatch.StartNew();
                for (int b = 0; b < merkleRoots.Count; b++)
                {
                    var batchId = Guid.NewGuid();
                    var instId = Guid.NewGuid();
                    var batchRoot = merkleRoots[b];

                    Console.WriteLine($"  [On-Chain] Anchoring Batch {b + 1}/{numBatches} (Root: {batchRoot.Substring(0, 16)}...)...");
                    var result = await blockchainService.AnchorMerkleRootAsync(batchId.ToString(), batchRoot, instId.ToString(), "Issue");
                    if (result.IsSuccess)
                    {
                        successfulOnChainTxs++;
                        Console.WriteLine($"  [On-Chain SUCCESS] Batch {b + 1}/{numBatches} anchored! TxHash: {result.Value.TransactionHash}");
                    }
                    else
                    {
                        Console.WriteLine($"  [On-Chain FAILED] Batch {b + 1}/{numBatches} failed: Code={result.Error.Code}, Msg={result.Error.Message}");
                    }
                }
                txSw.Stop();
                onChainTxDurationMs = txSw.ElapsedMilliseconds;
            }

            processSw.Stop();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryDelta = (finalMemory - initialMemory) / (1024.0 * 1024.0);

            var tps = count / (processSw.Elapsed.TotalSeconds > 0 ? processSw.Elapsed.TotalSeconds : 0.001);

            Console.WriteLine($"  [Result] Processed Degrees     : {count:N0}");
            Console.WriteLine($"  [Result] Total Batches (500/b) : {numBatches}");
            Console.WriteLine($"  [Result] Hashing Duration      : {hashSw.ElapsedMilliseconds} ms");
            Console.WriteLine($"  [Result] Merkle Build Duration : {merkleSw.ElapsedMilliseconds} ms");
            if (enableOnChain)
            {
                Console.WriteLine($"  [Result] On-Chain Tx Duration  : {onChainTxDurationMs} ms (Successful: {successfulOnChainTxs}/{numBatches})");
            }
            Console.WriteLine($"  [Result] Total Duration        : {processSw.ElapsedMilliseconds} ms");
            Console.WriteLine($"  [Result] Effective Throughput  : {tps:N2} degrees/sec");
            Console.WriteLine($"  [Result] Memory Consumption   : {memoryDelta:N2} MB");
            Console.WriteLine();
        }

        private static async Task RunBurstBenchmarkScenarioAsync(
            string name, 
            int ratePerSec, 
            int durationSeconds, 
            IHashService hashService, 
            IMerkleTreeService merkleService,
            IJsonCanonicalizer canonicalizer,
            IBlockchainService? blockchainService,
            bool enableOnChain)
        {
            Console.WriteLine($"--- Starting Scenario: {name} ---");
            int totalDegrees = ratePerSec * durationSeconds;
            var sw = Stopwatch.StartNew();

            for (int s = 0; s < durationSeconds; s++)
            {
                var secSw = Stopwatch.StartNew();
                var hashes = new List<string>();
                for (int i = 0; i < ratePerSec; i++)
                {
                    var canonicalJson = canonicalizer.Canonicalize(new { id = i, sec = s }).Value;
                    var salt = Guid.NewGuid().ToString("N");
                    hashes.Add(hashService.HashData(canonicalJson, salt).Value);
                }
                var tree = merkleService.BuildTree(hashes);
                secSw.Stop();

                string onChainInfo = "";
                if (enableOnChain && blockchainService != null)
                {
                    var batchId = Guid.NewGuid();
                    var instId = Guid.NewGuid();
                    var result = await blockchainService.AnchorMerkleRootAsync(batchId.ToString(), tree.MerkleRoot, instId.ToString(), "Issue");
                    if (result.IsSuccess)
                    {
                        onChainInfo = $" | On-Chain Tx: {result.Value.TransactionHash.Substring(0, 16)}...";
                    }
                    else
                    {
                        onChainInfo = $" | On-Chain Failed: {result.Error.Message}";
                    }
                }

                Console.WriteLine($"  Second {s + 1}/{durationSeconds}: Processed {ratePerSec} degrees in {secSw.ElapsedMilliseconds} ms (Merkle Root: {tree.MerkleRoot.Substring(0, 16)}...){onChainInfo}");
                await Task.Delay(Math.Max(0, 1000 - (int)secSw.ElapsedMilliseconds));
            }
            sw.Stop();

            Console.WriteLine($"  [Result] Total Burst Degrees   : {totalDegrees:N0}");
            Console.WriteLine($"  [Result] Total Burst Duration  : {sw.ElapsedMilliseconds} ms");
            Console.WriteLine();
        }
    }
}
