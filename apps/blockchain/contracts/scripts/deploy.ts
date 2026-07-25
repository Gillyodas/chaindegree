import { ethers } from "hardhat";
import * as fs from "fs";
import * as path from "path";

async function main() {
  const network = await ethers.provider.getNetwork();
  console.log(`Connecting to network: ${network.name}`);
  console.log(`Actual ChainId: ${network.chainId}`);

  // Fail-Fast: Verify target network ChainId matches expected network configuration
  const expectedChainIdMap: Record<string, bigint> = {
    besuLocal: 1337n,
    besuConsortium: 2026n,
    hardhat: 31337n
  };

  const expectedChainId = expectedChainIdMap[network.name] ?? (network.chainId === 2026n ? 2026n : 1337n);

  if (network.chainId !== expectedChainId) {
    console.error(`[ERROR] ChainId mismatch for network '${network.name}'! Expected ${expectedChainId}, got ${network.chainId}.`);
    process.exit(1);
  }
  console.log(`[OK] ChainId matches expected ChainId (${expectedChainId}) for network '${network.name}'.`);

  // 3. Deploy contract
  const DegreeAnchor = await ethers.getContractFactory("DegreeAnchor");
  console.log("Deploying DegreeAnchor...");
  const contract = await DegreeAnchor.deploy({ gasPrice: 0 });
  await contract.waitForDeployment();

  const contractAddress = await contract.getAddress();
  console.log(`[SUCCESS] DegreeAnchor deployed to: ${contractAddress}`);

  // 4. Authorize additional anchor signers (e.g., validator nodes / secondary signers) via env variable
  const additionalAnchorsRaw = process.env.ADDITIONAL_ANCHOR_ADDRESSES || process.env.SECONDARY_ANCHOR_ADDRESS;
  if (additionalAnchorsRaw) {
    const addresses = additionalAnchorsRaw.split(",").map((a) => a.trim()).filter((a) => a.length > 0);
    for (const address of addresses) {
      console.log(`Authorizing additional anchor signer: ${address}...`);
      const tx = await contract.addAnchorService(address, { gasPrice: 0 });
      await tx.wait();
      console.log(`[SUCCESS] Authorized additional anchor signer: ${address}`);
    }
  }

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
