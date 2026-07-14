import { expect } from "chai";
import { ethers } from "hardhat";
import { DegreeAnchor } from "../typechain-types";
import { SignerWithAddress } from "@nomicfoundation/hardhat-ethers/signers";

describe("DegreeAnchor", function () {
  let contract: DegreeAnchor;
  let owner: SignerWithAddress;
  let anchorService: SignerWithAddress;
  let unauthorizedUser: SignerWithAddress;

  beforeEach(async function () {
    [owner, anchorService, unauthorizedUser] = await ethers.getSigners();
    const DegreeAnchorFactory = await ethers.getContractFactory("DegreeAnchor");
    contract = (await DegreeAnchorFactory.deploy()) as DegreeAnchor;
    await contract.waitForDeployment();
  });

  describe("Deployment & Init", function () {
    it("Should set the correct owner", async function () {
      expect(await contract.owner()).to.equal(owner.address);
    });

    it("Should authorize the owner as an anchor service by default", async function () {
      expect(await contract.authorizedAnchors(owner.address)).to.be.true;
    });
  });

  describe("Admin Actions (Access Control)", function () {
    it("Should allow owner to authorize a new anchor service", async function () {
      await contract.addAnchorService(anchorService.address);
      expect(await contract.authorizedAnchors(anchorService.address)).to.be.true;
    });

    it("Should allow owner to remove an authorized anchor service", async function () {
      await contract.addAnchorService(anchorService.address);
      expect(await contract.authorizedAnchors(anchorService.address)).to.be.true;

      await contract.removeAnchorService(anchorService.address);
      expect(await contract.authorizedAnchors(anchorService.address)).to.be.false;
    });

    it("Should prevent non-owners from adding anchor services", async function () {
      await expect(
        contract.connect(unauthorizedUser).addAnchorService(anchorService.address)
      ).to.be.revertedWith("Not owner");
    });

    it("Should prevent non-owners from removing anchor services", async function () {
      await expect(
        contract.connect(unauthorizedUser).removeAnchorService(owner.address)
      ).to.be.revertedWith("Not owner");
    });
  });

  describe("Anchoring (anchorMerkleRoot)", function () {
    const batchId = ethers.keccak256(ethers.toUtf8Bytes("batch-123"));
    const merkleRoot = ethers.keccak256(ethers.toUtf8Bytes("merkle-root-abc"));
    const institutionId = ethers.keccak256(ethers.toUtf8Bytes("inst-1"));
    const actionType = "Issue";

    beforeEach(async function () {
      // Authorize anchorService for the anchoring tests
      await contract.addAnchorService(anchorService.address);
    });

    it("Should anchor a batch successfully and emit Event (Happy Path)", async function () {
      const tx = await contract
        .connect(anchorService)
        .anchorMerkleRoot(batchId, merkleRoot, institutionId, actionType);

      await expect(tx)
        .to.emit(contract, "BatchAnchored")
        .withArgs(batchId, merkleRoot, (anyTimestamp: any) => typeof anyTimestamp === "bigint" || anyTimestamp >= 0n);

      // Verify state
      const batch = await contract.batches(batchId);
      expect(batch.MerkleRoot).to.equal(merkleRoot);
      expect(batch.InstitutionId).to.equal(institutionId);
      expect(batch.ActionType).to.equal(actionType);
      expect(batch.Exists).to.be.true;
      expect(batch.Timestamp).to.be.gt(0n);
    });

    it("Should fail if caller is not an authorized anchor service (Unauthorized Caller)", async function () {
      await expect(
        contract
          .connect(unauthorizedUser)
          .anchorMerkleRoot(batchId, merkleRoot, institutionId, actionType)
      ).to.be.revertedWith("Not authorized anchor");
    });

    it("Should fail if anchoring a duplicate batch ID (Duplicate Batch)", async function () {
      // First anchor
      await contract
        .connect(anchorService)
        .anchorMerkleRoot(batchId, merkleRoot, institutionId, actionType);

      // Second duplicate anchor
      await expect(
        contract
          .connect(anchorService)
          .anchorMerkleRoot(batchId, merkleRoot, institutionId, actionType)
      ).to.be.revertedWith("Batch already anchored");
    });

    it("Should return default values for non-existing batches (Non-existing Batch)", async function () {
      const nonExistentBatchId = ethers.keccak256(ethers.toUtf8Bytes("non-existing-batch"));
      const batch = await contract.batches(nonExistentBatchId);
      
      expect(batch.MerkleRoot).to.equal(ethers.ZeroHash);
      expect(batch.Exists).to.be.false;
      expect(batch.Timestamp).to.equal(0n);
    });
  });
});
