import { describe, it, expect } from 'vitest';
import { HttpError } from '@/shared/api/http';
import { mapVerificationResponse, mapVerificationError } from '../verification.mapper';
import type { VerifyDegreeSuccessResponse } from '../verification.types';

describe('verification.mapper', () => {
  const baseSuccessData: VerifyDegreeSuccessResponse = {
    verified: true,
    status: 'Confirmed',
    verificationSource: 'Blockchain_Merkle_Root',
    degreeCode: 'DEG-2026-000001',
    version: 1,
    institutionName: 'State University of Technology',
    studentFullName: 'Alice Nguyen',
    major: 'Computer Science',
    classification: 'Excellent',
    issuedAt: '2026-06-15T08:00:00Z',
    blockchain: {
      txHash: '0x1234567890abcdef1234567890abcdef12345678',
      blockNumber: 123456,
      merkleRoot: '0xabcdef123456',
      merkleProofJson: null,
    },
  };

  describe('mapVerificationResponse', () => {
    it('returns kind: valid for verified Confirmed degree', () => {
      const result = mapVerificationResponse(baseSuccessData);
      expect(result.kind).toBe('valid');
      if (result.kind === 'valid') {
        expect(result.data.degreeCode).toBe('DEG-2026-000001');
        expect(result.data.status).toBe('Confirmed');
      }
    });

    it('returns kind: revoked when status is Revoked even if verified is true', () => {
      const revokedData: VerifyDegreeSuccessResponse = {
        ...baseSuccessData,
        status: 'Revoked',
      };
      const result = mapVerificationResponse(revokedData);
      expect(result.kind).toBe('revoked');
      if (result.kind === 'revoked') {
        expect(result.data.status).toBe('Revoked');
      }
    });

    it('defensively falls back to valid if status is missing', () => {
      const dataWithoutStatus = {
        ...baseSuccessData,
        status: undefined,
      } as unknown as VerifyDegreeSuccessResponse;
      const result = mapVerificationResponse(dataWithoutStatus);
      expect(result.kind).toBe('valid');
    });
  });

  describe('mapVerificationError', () => {
    it('maps DEGREE_NOT_FOUND HttpError to kind: not_found', () => {
      const error = new HttpError(
        'not_found',
        404,
        'No degree found with the specified code.',
        'DEGREE_NOT_FOUND',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'not_found',
        errorCode: 'DEGREE_NOT_FOUND',
        message: 'No degree found with the specified code.',
      });
    });

    it('maps UNSUPPORTED_VERSION HttpError to kind: not_found', () => {
      const error = new HttpError(
        'not_found',
        404,
        'The specified version does not exist.',
        'UNSUPPORTED_VERSION',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'not_found',
        errorCode: 'UNSUPPORTED_VERSION',
        message: 'The specified version does not exist.',
      });
    });

    it('maps CRYPTO_HASH_MISMATCH HttpError to kind: tampered', () => {
      const error = new HttpError(
        'validation',
        422,
        'Verification failed. Hash mismatch.',
        'CRYPTO_HASH_MISMATCH',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'tampered',
        errorCode: 'CRYPTO_HASH_MISMATCH',
        message: 'Verification failed. Hash mismatch.',
      });
    });

    it('maps BLOCKCHAIN_INVALID HttpError to kind: tampered', () => {
      const error = new HttpError(
        'validation',
        422,
        'Blockchain verification failed.',
        'BLOCKCHAIN_INVALID',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'tampered',
        errorCode: 'BLOCKCHAIN_INVALID',
        message: 'Blockchain verification failed.',
      });
    });

    it('maps INVALID_SALT_FORMAT HttpError to kind: input_error', () => {
      const error = new HttpError(
        'validation',
        400,
        'Salt must be 16-char hex.',
        'INVALID_SALT_FORMAT',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'input_error',
        errorCode: 'INVALID_SALT_FORMAT',
        message: 'Salt must be 16-char hex.',
      });
    });

    it('maps HTTP 429 status to kind: rate_limited', () => {
      const error = new HttpError(
        'server_error',
        429,
        'Too many requests',
      );
      const result = mapVerificationError(error);
      expect(result.kind).toBe('rate_limited');
      if (result.kind === 'rate_limited') {
        expect(result.message).toContain('Too many verification requests');
      }
    });

    it('maps HTTP 500 server_error to kind: server_error', () => {
      const error = new HttpError(
        'server_error',
        500,
        'Internal server error',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'server_error',
        message: 'Internal server error',
      });
    });

    it('maps timeout HttpError to kind: server_error', () => {
      const error = new HttpError(
        'timeout',
        null,
        'Request timed out. Please check your connection and try again.',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'server_error',
        message: 'Request timed out. Please check your connection and try again.',
      });
    });

    it('maps network HttpError to kind: server_error', () => {
      const error = new HttpError(
        'network',
        null,
        'Unable to connect to the server.',
      );
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'server_error',
        message: 'Unable to connect to the server.',
      });
    });

    it('gracefully handles unknown generic Error object without crashing', () => {
      const error = new Error('Random unexpected client runtime error');
      const result = mapVerificationError(error);
      expect(result).toEqual({
        kind: 'server_error',
        message: 'Random unexpected client runtime error',
      });
    });

    it('gracefully handles non-Error thrown objects (string/null)', () => {
      const result = mapVerificationError('string error');
      expect(result).toEqual({
        kind: 'server_error',
        message: 'An unexpected error occurred. Please try again.',
      });
    });
  });
});
