# AI Context - ChainDegree

> This document is the entry point for any AI assistant working on this repository.
> Read this file first before reviewing code or proposing architecture changes.

---

# Project Overview

**Project:** ChainDegree

ChainDegree is a blockchain-based digital degree management system.

Primary goals:

- Issue digital degrees
- Store business data in SQL Server
- Anchor Merkle Root on Hyperledger Besu
- Public verification
- Enterprise-oriented architecture

---

# Tech Stack

Backend

- .NET 10
- ASP.NET Core
- Clean Architecture
- DDD
- CQRS
- SQL Server
- EF Core

Blockchain

- Hyperledger Besu
- QBFT Consensus
- Nethereum
- Solidity
- Hardhat

Infrastructure

- Docker
- Docker Compose

---

# Architecture Principles

The project follows these principles.

1. Security First

Security always has higher priority than convenience.

2. Correctness First

The system must always produce correct data before being optimized.

3. Simplicity

Avoid unnecessary abstractions.

Only introduce complexity when there is a real requirement.

4. Maintainability

Code should be easy to understand for future developers.

5. Domain Driven Design

Business language belongs to Application layer.

Infrastructure details must stay inside Infrastructure.

---

# Current Progress

Completed

- Phase 0
    - Local Besu
    - Hardhat
    - Smart Contract
    - Unit Tests
    - Deployment Script
    - Smoke Test

Current Phase

- Phase 1
- Nethereum Integration

Current Work Package

(Update this manually)

Example

WP 1.3
Implement NethereumBlockchainService

---

# Architecture Decisions

All important decisions are stored in:

docs/adr/

AI should never change those decisions unless explicitly requested.

---

# Blockchain Principles

Current blockchain is:

Hyperledger Besu (QBFT)

Meaning:

- Transaction Receipt is considered Finalized.
- No N-block confirmation strategy is required.
- Mapping is the source of truth.
- Event Log is NOT used for Worker business logic.

Worker should verify:

contract.batches(batchId)

instead of

Event Logs.

---

# Error Handling Principles

Business failures

↓

Use Result Pattern.

Infrastructure failures that can recover

↓

Use Result Pattern.

Examples

- RPC timeout
- Network failure
- HTTP 503
- Contract revert
- Unauthorized signer

Programming errors

↓

Throw Exception.

Never wrap programming bugs inside Result.

Examples

- NullReferenceException
- InvalidCastException
- ArgumentOutOfRangeException

Startup configuration errors

↓

Fail Fast.

Never allow application to start with invalid blockchain configuration.

---

# Coding Philosophy

Prefer

Simple > Clever

Explicit > Implicit

Composition > Inheritance

Readability > Short code

Correctness > Performance

---

# AI Review Rules

When reviewing code:

1.
Do NOT introduce over-engineering.

2.
Prefer existing .NET patterns before inventing custom solutions.

3.
Prefer Nethereum built-in APIs instead of reimplementing blockchain protocols.

4.
Keep Application independent from Infrastructure.

5.
Question unnecessary abstractions.

6.
Always consider security implications first.

7.
Every recommendation should explain the trade-offs.

---

# Repository Structure

docs/

src/

tests/

---

# Documentation Priority

When conflicts happen, follow this order.

1.
ADR

2.
Architecture Documents

3.
Implementation Plan

4.
Code Comments

5.
AI Suggestions

AI must never override an ADR without strong technical justification.