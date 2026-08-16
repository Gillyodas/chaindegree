import { HttpError } from '@/shared/api/http';
import type {
  VerifyDegreeSuccessResponse,
  VerificationResultType,
  VerificationErrorCode,
} from './verification.types';

export function mapVerificationResponse(
  data: VerifyDegreeSuccessResponse,
): VerificationResultType {
  if (data?.status === 'Revoked') {
    return {
      kind: 'revoked',
      data,
    };
  }

  return {
    kind: 'valid',
    data,
  };
}

export function mapVerificationError(error: unknown): VerificationResultType {
  if (error instanceof HttpError) {
    if (error.status === 429) {
      return {
        kind: 'rate_limited',
        message: 'Too many verification requests. Please wait a moment and try again.',
      };
    }

    if (error.errorCode) {
      switch (error.errorCode) {
        case 'DEGREE_NOT_FOUND':
        case 'UNSUPPORTED_VERSION':
          return {
            kind: 'not_found',
            errorCode: error.errorCode,
            message: error.message,
          };

        case 'CRYPTO_HASH_MISMATCH':
        case 'BLOCKCHAIN_INVALID':
          return {
            kind: 'tampered',
            errorCode: error.errorCode as VerificationErrorCode,
            message: error.message,
          };

        case 'INVALID_SALT_FORMAT':
          return {
            kind: 'input_error',
            errorCode: error.errorCode,
            message: error.message,
          };
      }
    }

    return {
      kind: 'server_error',
      message: error.message || 'Something went wrong on our end. Please try again later.',
    };
  }

  if (error instanceof Error) {
    return {
      kind: 'server_error',
      message: error.message,
    };
  }

  return {
    kind: 'server_error',
    message: 'An unexpected error occurred. Please try again.',
  };
}
