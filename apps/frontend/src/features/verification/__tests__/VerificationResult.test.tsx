import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { VerificationResult } from '../components/VerificationResult';
import type { VerificationResultType, VerifyDegreeSuccessResponse } from '../verification.types';

describe('VerificationResult', () => {
  const baseSuccessData: VerifyDegreeSuccessResponse = {
    verified: true,
    status: 'Confirmed',
    verificationSource: 'Blockchain_Merkle_Root',
    degreeCode: 'DEG-2026-000001',
    version: 1,
    institutionName: 'State University',
    studentFullName: 'John Doe',
    major: 'Computer Science',
    classification: 'Very Good',
    issuedAt: '2026-06-15T08:00:00Z',
    blockchain: {
      txHash: '0x1234567890abcdef1234567890abcdef12345678',
      blockNumber: 42,
      merkleRoot: '0xroot',
      merkleProofJson: null,
    },
  };

  it('renders nothing when result is null', () => {
    const { container } = render(<VerificationResult result={null} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('renders VerifiedResult when result is valid', () => {
    const result: VerificationResultType = {
      kind: 'valid',
      data: baseSuccessData,
    };
    render(<VerificationResult result={result} />);

    expect(screen.getByText('Degree Verified & Valid')).toBeInTheDocument();
    expect(screen.getByText('State University')).toBeInTheDocument();
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('Computer Science')).toBeInTheDocument();
    expect(screen.getByText('Confirmed (v1)')).toBeInTheDocument();
    expect(screen.getByText('#42')).toBeInTheDocument();
  });

  it('renders RevokedResult when result is revoked', () => {
    const result: VerificationResultType = {
      kind: 'revoked',
      data: { ...baseSuccessData, status: 'Revoked' },
    };
    render(<VerificationResult result={result} />);

    expect(screen.getByText('Degree Revoked')).toBeInTheDocument();
    expect(screen.getByText('Revoked')).toBeInTheDocument();
    expect(screen.getByText(/status has been revoked by the issuing institution/i)).toBeInTheDocument();
  });

  it('renders TamperedWarning when kind is tampered with CRYPTO_HASH_MISMATCH', () => {
    const result: VerificationResultType = {
      kind: 'tampered',
      errorCode: 'CRYPTO_HASH_MISMATCH',
      message: 'Hash mismatch between data and ledger',
    };
    render(<VerificationResult result={result} />);

    expect(screen.getByText('Integrity Verification Failed')).toBeInTheDocument();
    expect(screen.getByText(/CRITICAL WARNING: Cryptographic Hash Mismatch/i)).toBeInTheDocument();
    expect(screen.getByText('CRYPTO_HASH_MISMATCH')).toBeInTheDocument();
  });

  it('renders TamperedWarning when kind is tampered with BLOCKCHAIN_INVALID', () => {
    const result: VerificationResultType = {
      kind: 'tampered',
      errorCode: 'BLOCKCHAIN_INVALID',
      message: 'Blockchain node validation failed',
    };
    render(<VerificationResult result={result} />);

    expect(screen.getByText('Integrity Verification Failed')).toBeInTheDocument();
    expect(screen.getByText(/CRITICAL WARNING: Blockchain Ledger Validation Failed/i)).toBeInTheDocument();
    expect(screen.getByText('BLOCKCHAIN_INVALID')).toBeInTheDocument();
  });

  it('renders NotFoundResult when kind is not_found with DEGREE_NOT_FOUND', () => {
    const result: VerificationResultType = {
      kind: 'not_found',
      errorCode: 'DEGREE_NOT_FOUND',
      message: 'Degree code not found',
    };
    render(<VerificationResult result={result} />);

    expect(screen.getByText('Degree Record Not Found')).toBeInTheDocument();
    expect(screen.getByText(/No degree found with the provided degree code/i)).toBeInTheDocument();
  });

  it('renders NotFoundResult when kind is not_found with UNSUPPORTED_VERSION', () => {
    const result: VerificationResultType = {
      kind: 'not_found',
      errorCode: 'UNSUPPORTED_VERSION',
      message: 'Version not found',
    };
    render(<VerificationResult result={result} />);

    expect(screen.getByText('Degree Version Not Found')).toBeInTheDocument();
    expect(screen.getByText(/The specified version number was not found/i)).toBeInTheDocument();
  });

  it('renders VerificationError when kind is rate_limited', () => {
    const result: VerificationResultType = {
      kind: 'rate_limited',
      message: 'Too many verification requests. Please wait a moment.',
    };
    render(<VerificationResult result={result} />);

    expect(screen.getByText('Rate Limit Exceeded')).toBeInTheDocument();
    expect(screen.getByText('Rate Limited')).toBeInTheDocument();
    expect(screen.getByText('Too many verification requests. Please wait a moment.')).toBeInTheDocument();
  });

  it('renders VerificationError with Retry button when kind is server_error', () => {
    const onRetry = vi.fn();
    const result: VerificationResultType = {
      kind: 'server_error',
      message: 'Database connection failed.',
    };
    render(<VerificationResult result={result} onRetry={onRetry} />);

    expect(screen.getByText('Verification Request Failed')).toBeInTheDocument();
    expect(screen.getByText('Database connection failed.')).toBeInTheDocument();

    const retryBtn = screen.getByRole('button', { name: /try again/i });
    fireEvent.click(retryBtn);
    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
