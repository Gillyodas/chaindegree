import { ethers } from "hardhat";
import * as fs from "fs";
import * as path from "path";

async function main() {
  const EXPECTED_CHAIN_ID = 1337n;

  // 1. Fetch current network details
  const network = await ethers.provider.getNetwork();
  console.log(`Connecting to network: ${network.name}`);
  console.log(`Actual ChainId: ${network.chainId}`);

  // 2. Fail Fast: Check ChainId
  if (network.chainId !== EXPECTED_CHAIN_ID) {
    console.error(`[ERROR] ChainId mismatch! Expected ${EXPECTED_CHAIN_ID}, got ${network.chainId}.`);
    process.exit(1);
  }
  console.log("[OK] ChainId matches expected development ChainId (1337).");

  // 3. Deploy contract
  const DegreeAnchor = await ethers.getContractFactory("DegreeAnchor");
  console.log("Deploying DegreeAnchor...");
  const contract = await DegreeAnchor.deploy();
  await contract.waitForDeployment();

  const contractAddress = await contract.getAddress();
  console.log(`[SUCCESS] DegreeAnchor deployed to: ${contractAddress}`);

  // 4. Save contract address to a local JSON file for downstream integration (Smoke test & backend config)
  const deployInfoPath = path.join(__dirname, "../deployed-address.json");
  fs.writeFileSync(
    deployInfoPath,
    JSON.stringify({ address: contractAddress, chainId: network.chainId.toString() }, null, 2)
  );
  console.log(`Saved deployment info to: ${deployInfoPath}`);
}

main()
  .then(() => process.exit(0))
  .catch((error) => {
    console.error(error);
    process.exit(1);
  });
