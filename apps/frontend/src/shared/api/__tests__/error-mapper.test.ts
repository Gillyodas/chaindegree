import { describe, it, expect } from 'vitest';
import { getErrorMessage, getBusinessErrorMessage } from '../error-mapper';
import { HttpError } from '../http';

describe('error-mapper', () => {
  describe('getBusinessErrorMessage', () => {
    it('should map known business error codes to correct English messages', () => {
      expect(getBusinessErrorMessage('DEGREE_ALREADY_EXISTS')).toBe(
        'A degree with identical details has already been issued for this student.',
      );
      expect(getBusinessErrorMessage('CRYPTO_HASH_MISMATCH')).toBe(
        'Verification failed. The provided data does not match official records.',
      );
      expect(getBusinessErrorMessage('BLOCKCHAIN_INVALID')).toBe(
        'Blockchain verification failed. Data integrity cannot be confirmed.',
      );
      expect(getBusinessErrorMessage('DEGREE_NOT_FOUND')).toBe(
        'No degree found with the specified code.',
      );
      expect(getBusinessErrorMessage('UNSUPPORTED_VERSION')).toBe(
        'The specified degree version is not supported.',
      );
      expect(getBusinessErrorMessage('FILTER_CRITERIA_NOT_SATISFIED')).toBe(
        'Your degree does not meet the minimum requirements for this position.',
      );
      expect(getBusinessErrorMessage('Report.EvidenceRequired')).toBe(
        'Evidence file is required when submitting a report.',
      );
    });

    it('should return null for unknown error codes', () => {
      expect(getBusinessErrorMessage('UNKNOWN_CODE_123')).toBeNull();
      expect(getBusinessErrorMessage(undefined)).toBeNull();
    });
  });

  describe('getErrorMessage', () => {
    it('should map HttpError with business error code to mapped message', () => {
      const err = new HttpError(
        'conflict',
        409,
        'Original message',
        'DEGREE_ALREADY_EXISTS',
      );
      expect(getErrorMessage(err)).toBe(
        'A degree with identical details has already been issued for this student.',
      );
    });

    it('should produce 3 distinct messages for server_error, timeout, and network errors', () => {
      const serverErr = new HttpError('server_error', 500, 'Something went wrong on our end. Please try again later.');
      const timeoutErr = new HttpError('timeout', null, 'Request timed out. Please check your connection and try again.');
      const networkErr = new HttpError('network', null, 'Unable to connect to the server. Please check your internet connection.');

      const msg1 = getErrorMessage(serverErr);
      const msg2 = getErrorMessage(timeoutErr);
      const msg3 = getErrorMessage(networkErr);

      expect(msg1).not.toBe(msg2);
      expect(msg2).not.toBe(msg3);
      expect(msg1).not.toBe(msg3);

      expect(msg1).toContain('Something went wrong on our end');
      expect(msg2).toContain('timed out');
      expect(msg3).toContain('Unable to connect to the server');
    });

    it('should preserve not_found error type on HttpError without forcing global empty state', () => {
      const notFoundErr = new HttpError('not_found', 404, 'The requested resource was not found.');
      expect(notFoundErr.type).toBe('not_found');
      expect(notFoundErr.status).toBe(404);
      expect(getErrorMessage(notFoundErr)).toBe('The requested resource was not found.');
    });

    it('should return fallback message for unknown error types', () => {
      expect(getErrorMessage('some string error')).toBe('An unexpected error occurred. Please try again.');
      expect(getErrorMessage(null)).toBe('An unexpected error occurred. Please try again.');
    });
  });
});
