import { ethers } from "hardhat";
import * as fs from "fs";
import * as path from "path";

async function main() {
  // 1. Read contract address
  const deployInfoPath = path.join(__dirname, "../deployed-address.json");
  if (!fs.existsSync(deployInfoPath)) {
    console.error(`[ERROR] Deployment info not found at ${deployInfoPath}. Run deploy script first.`);
    process.exit(1);
  }

  const deployInfo = JSON.parse(fs.readFileSync(deployInfoPath, "utf8"));
  const contractAddress = deployInfo.address;
  console.log(`Read deployed contract address: ${contractAddress}`);

  // 2. Verify contract exists
  console.log("Verifying if contract exists on chain...");
  const code = await ethers.provider.getCode(contractAddress);
  if (code === "0x") {
    console.error(`[ERROR] No contract code found at address ${contractAddress}! Deploy failed or wrong address.`);
    process.exit(1);
  }
  console.log(`[OK] Contract exists! Bytecode length: ${code.length} characters.`);

  // 3. Bind contract instance
  const DegreeAnchor = await ethers.getContractFactory("DegreeAnchor");
  const contract = DegreeAnchor.attach(contractAddress) as any;

  // 4. Generate dummy inputs
  const batchId = ethers.keccak256(ethers.toUtf8Bytes(`smoke-batch-${Date.now()}`));
  const merkleRoot = ethers.keccak256(ethers.toUtf8Bytes("smoke-merkle-root"));
  const institutionId = ethers.keccak256(ethers.toUtf8Bytes("smoke-inst"));
  const actionType = "Issue";

  console.log(`\nStarting Smoke Test with BatchId: ${batchId}`);
  console.log(`Expected MerkleRoot: ${merkleRoot}`);

  // 5. Send transaction
  console.log("Sending anchorMerkleRoot transaction...");
  const tx = await contract.anchorMerkleRoot(batchId, merkleRoot, institutionId, actionType);
  console.log(`Transaction sent. Hash: ${tx.hash}`);

  // 6. Wait for block confirmation (Receipt)
  console.log("Waiting for block confirmation...");
  const receipt = await tx.wait();
  console.log(`[SUCCESS] Transaction mined in block: ${receipt.blockNumber}`);

  // 7. Verify state mapping on-chain
  console.log("Querying batches mapping on-chain...");
  const batch = await contract.batches(batchId);
  
  console.log(`On-chain MerkleRoot: ${batch.MerkleRoot}`);
  console.log(`On-chain Exists flag: ${batch.Exists}`);
  console.log(`On-chain Timestamp: ${batch.Timestamp}`);

  // 8. Assertions
  if (batch.MerkleRoot !== merkleRoot) {
    console.error(`[FAIL] MerkleRoot mismatch! Expected ${merkleRoot}, got ${batch.MerkleRoot}`);
    process.exit(1);
  }

  if (!batch.Exists) {
    console.error("[FAIL] Exists flag is false!");
    process.exit(1);
  }

  console.log("\n[PASS] Smoke Test Hoàn Hảo - Merkle Root anchored and verified successfully!");
}

main()
  .then(() => process.exit(0))
  .catch((error) => {
    console.error(error);
    process.exit(1);
  });
