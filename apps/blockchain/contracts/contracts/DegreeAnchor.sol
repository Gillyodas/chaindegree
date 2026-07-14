// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

contract DegreeAnchor {
    
    address public owner;
    mapping(address => bool) public authorizedAnchors;
    
    struct BatchMetadata {
        bytes32 MerkleRoot;
        uint256 Timestamp;
        bytes32 InstitutionId;
        string ActionType; // "Issue", "Update", "Revoke"
        bool Exists;
    }

    // Mapping: batchId → BatchMetadata
    mapping(bytes32 => BatchMetadata) public batches;
    
    modifier onlyOwner() {
        require(msg.sender == owner, "Not owner");
        _;
    }
    
    modifier onlyAnchorService() {
        require(authorizedAnchors[msg.sender], "Not authorized anchor");
        _;
    }
    
    event BatchAnchored(
        bytes32 indexed batchId,
        bytes32 merkleRoot,
        uint256 timestamp
    );
    
    constructor() {
        owner = msg.sender;
        authorizedAnchors[msg.sender] = true;
    }
    
    function addAnchorService(address _service) external onlyOwner {
        authorizedAnchors[_service] = true;
    }
    
    function removeAnchorService(address _service) external onlyOwner {
        authorizedAnchors[_service] = false;
    }
    
    function anchorMerkleRoot(
        bytes32 batchId, 
        bytes32 merkleRoot,
        bytes32 institutionId,
        string calldata actionType
    ) external onlyAnchorService {
        require(!batches[batchId].Exists, "Batch already anchored");
        
        batches[batchId] = BatchMetadata({
            MerkleRoot: merkleRoot,
            Timestamp: block.timestamp,
            InstitutionId: institutionId,
            ActionType: actionType,
            Exists: true
        });
        
        emit BatchAnchored(batchId, merkleRoot, block.timestamp);
    }
}
