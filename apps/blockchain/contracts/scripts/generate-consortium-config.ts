import * as fs from "fs";
import * as path from "path";
import { ethers } from "hardhat";

async function main() {
  console.log("Generating Consortium configurations (4 Validators + 1 RPC)...");

  // Create configuration folders if they don't exist
  const rootBlockchainDir = path.join(__dirname, "..", "..");
  const genesisDir = path.join(rootBlockchainDir, "genesis");
  const configsDir = path.join(rootBlockchainDir, "configs");

  fs.mkdirSync(genesisDir, { recursive: true });
  fs.mkdirSync(configsDir, { recursive: true });

  const nodeTypes = ["validator1", "validator2", "validator3", "validator4", "rpc"];
  const nodeKeys: { [key: string]: { privateKey: string; address: string; enodePubKey: string } } = {};

  for (const node of nodeTypes) {
    const nodeDir = path.join(configsDir, node);
    fs.mkdirSync(nodeDir, { recursive: true });

    // Generate standard SECP256K1 keypair
    const wallet = ethers.Wallet.createRandom();
    
    // Besu private key files are raw hex strings WITHOUT 0x prefix
    const rawPrivateKey = wallet.privateKey.startsWith("0x")
      ? wallet.privateKey.slice(2)
      : wallet.privateKey;

    fs.writeFileSync(path.join(nodeDir, "key"), rawPrivateKey, "utf8");

    // Ethers v6: compute uncompressed public key starting with 0x04.
    // Besu enode needs the raw 64-byte uncompressed public key (128 hex characters) without 0x04 or 0x.
    const signingKey = new ethers.SigningKey(wallet.privateKey);
    const uncompressedPubKey = signingKey.publicKey;
    const enodePubKey = uncompressedPubKey.startsWith("0x04")
      ? uncompressedPubKey.slice(4)
      : uncompressedPubKey.slice(2); // Fallback in case prefix is different

    nodeKeys[node] = {
      privateKey: wallet.privateKey,
      address: wallet.address,
      enodePubKey: enodePubKey,
    };

    console.log(`- Generated key for ${node}: Address ${wallet.address}`);
  }

  // Create static-nodes.json
  // We use the docker service names as hostnames in the docker compose network
  const staticNodes = [
    `enode://${nodeKeys["validator1"].enodePubKey}@besu-validator1:30303`,
    `enode://${nodeKeys["validator2"].enodePubKey}@besu-validator2:30303`,
    `enode://${nodeKeys["validator3"].enodePubKey}@besu-validator3:30303`,
    `enode://${nodeKeys["validator4"].enodePubKey}@besu-validator4:30303`,
    `enode://${nodeKeys["rpc"].enodePubKey}@besu-rpc:30303`,
  ];

  fs.writeFileSync(
    path.join(configsDir, "static-nodes.json"),
    JSON.stringify(staticNodes, null, 2),
    "utf8"
  );
  console.log("Created configs/static-nodes.json");

  // Build QBFT extraData RLP: RLP([32-bytes Vanity, List<Validators>, Vote, Round, Seals])
  const vanity = ethers.zeroPadValue("0x", 32); // 32 bytes of zeros
  const validatorsList = [
    nodeKeys["validator1"].address,
    nodeKeys["validator2"].address,
    nodeKeys["validator3"].address,
    nodeKeys["validator4"].address,
  ];

  // RLP encode the QBFT Extra Data:
  // - vanity: 32 bytes hex
  // - validatorsList: array of addresses
  // - vote: 0x (null)
  // - round: 0x (null or empty list/bytes)
  // - seals: [] (empty list)
  const extraDataRlp = ethers.encodeRlp([
    vanity,
    validatorsList,
    "0x",
    "0x",
    []
  ]);

  console.log(`Generated QBFT extraData: ${extraDataRlp}`);

  // Create genesis.json
  const genesis = {
    config: {
      chainId: 2026,
      constantinopleBlock: 0,
      petersburgBlock: 0,
      zeroBaseFee: true,
      qbft: {
        blockperiodseconds: 2,
        epochlength: 30000,
        requesttimeoutseconds: 10
      }
    },
    nonce: "0x0",
    timestamp: "0x58ee40ba",
    gasLimit: "0x1fffffffffffff",
    difficulty: "0x1",
    mixHash: "0x63746963616c2062797a616e74696e65206661756c7420746f6c6572616e6365",
    coinbase: "0x0000000000000000000000000000000000000000",
    alloc: {
      // Pre-fund the owner/deployer account so they can deploy contracts if needed
      // Pre-fund all validators and rpc nodes
      [nodeKeys["validator1"].address]: { balance: "0xad78ebc5ac6200000" }, // 2000 ETH in hex
      [nodeKeys["validator2"].address]: { balance: "0xad78ebc5ac6200000" },
      [nodeKeys["validator3"].address]: { balance: "0xad78ebc5ac6200000" },
      [nodeKeys["validator4"].address]: { balance: "0xad78ebc5ac6200000" },
      [nodeKeys["rpc"].address]: { balance: "0xad78ebc5ac6200000" },
      // Standard hardhat pre-funded addresses (like the standard dev coinbase/owner)
      "0xfe3b557e8fb62b89f4916b721be55ceb828dbd73": { balance: "0xad78ebc5ac6200000" },
      // Whitelisted deployer / local private key address (0x8f2a55949038a9610f50fb23b5883af3b4ecb3c3bb792cbcefbd1542c692be63 => 0xfe3b557e8fb62b89f4916b721be55ceb828dbd73)
    },
    extraData: extraDataRlp
  };

  fs.writeFileSync(
    path.join(genesisDir, "genesis.json"),
    JSON.stringify(genesis, null, 2),
    "utf8"
  );
  console.log("Created genesis/genesis.json");
  console.log("Consortium config generation complete.");
}

main()
  .then(() => process.exit(0))
  .catch((error) => {
    console.error(error);
    process.exit(1);
  });
