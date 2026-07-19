# Coding Standards

This document defines the coding conventions used throughout the ChainDegree project.

---

# General Principles

Write code for humans.

Not for compilers.

Prioritize:

Correctness

↓

Readability

↓

Maintainability

↓

Performance

---

# SOLID

Follow SOLID when it improves maintainability.

Do not force SOLID everywhere.

Avoid creating interfaces "just in case".

---

# Clean Architecture

Application

- Business rules
- Interfaces
- Result pattern
- Domain language

Infrastructure

- EF Core
- Nethereum
- SQL
- HTTP
- Docker
- External services

Application must never depend on Infrastructure.

---

# Result Pattern

Use Result for:

- Validation failures
- Business rule violations
- Recoverable infrastructure failures

Examples

✓ Degree already exists

✓ RPC timeout

✓ Network unavailable

✓ Unauthorized signer

Do NOT use Result for:

- NullReferenceException
- InvalidCastException
- Programming bugs

---

# Exception Policy

Catch only exceptions that you know how to handle.

Never write

catch (Exception)

unless rethrowing or adding context.

Allowed examples

- RpcResponseException
- HttpRequestException
- SocketException
- TaskCanceledException

Unexpected exceptions should crash.

---

# Startup Validation

Application must fail fast.

Validate:

- Configuration
- ChainId
- Contract existence
- Signer authorization

Never continue startup if blockchain configuration is invalid.

---

# Dependency Injection

Always inject abstractions.

Avoid Service Locator.

Avoid static services.

Prefer constructor injection.

---

# Logging

Logs must contain enough information for production debugging.

Blockchain operations should include:

CorrelationId

BatchId

BlockchainTxHash

Elapsed Time

Never log:

Private Keys

Secrets

Passwords

Tokens

---

# Async

Prefer async all the way.

Never block async code.

Avoid:

.Result

.Wait()

---

# CancellationToken

Every async public method should accept CancellationToken.

Pass CancellationToken to downstream APIs whenever possible.

---

# Naming

Use domain language.

Good

AnchorMerkleRootAsync

CheckBatchExistsAsync

DegreeProcessingRecord

Bad

Execute()

Run()

Process()

Helper()

Manager()

Util()

---

# Blockchain

Never implement blockchain protocols manually.

Use Nethereum built-in APIs whenever available.

Use:

Account

TransactionManager

ContractHandler

FunctionMessage

Do NOT manually:

Sign RLP

Encode ABI

Build raw transaction

Unless there is a real requirement.

---

# Smart Contract

Storage is the source of truth.

Worker should read:

mapping

Worker should NOT rely on:

Event Logs

Events exist for:

Explorer

Analytics

Audit

UI

---

# Retry Policy

Retry only transient failures.

Examples

✓ Network timeout

✓ HTTP 503

✓ RPC unavailable

Never retry

Invalid input

Unauthorized

Contract revert caused by business rules

---

# Idempotency

Never send duplicate blockchain transactions.

Always check

1.

Existing TxHash

↓

2.

On-chain State

↓

3.

Send New Transaction

---

# Security

Never store secrets in source code.

Development

↓

.env

Production

↓

KMS / Remote Signer

Never expose RPC publicly.

Validator nodes should not expose RPC.

---

# Testing

Every feature should be testable.

Prefer

Unit Test

↓

Integration Test

↓

Manual Test

Every completed phase should have

Done Criteria

Deliverables

Verification Plan

---

# Documentation

Every significant architectural decision should have an ADR.

Code comments explain

HOW

ADR explains

WHY

Implementation Plan explains

WHEN

Runbook explains

HOW TO OPERATE

---

# Golden Rule

Whenever making a design decision, ask:

Is this simpler?

Is this more secure?

Is this easier to maintain?

If not,

do not introduce it.