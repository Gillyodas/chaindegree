export interface VerifyDegreeRequest {
  degreeCode: string;
  version?: number | null;
  issuedAt?: string | null;
  plainDataJson?: string | null;
  salt?: string | null;
}

export interface BlockchainDetails {
  txHash: string;
  blockNumber: number | null;
  merkleRoot: string;
  merkleProofJson: string | null;
}

export interface VerifyDegreeSuccessResponse {
  verified: boolean;
  status: string;
  verificationSource: string | null;
  degreeCode: string;
  version: number;
  institutionName: string | null;
  studentFullName: string | null;
  major: string | null;
  classification: string | null;
  issuedAt: string | null;
  blockchain: BlockchainDetails | null;
}

export type VerificationErrorCode =
  | 'DEGREE_NOT_FOUND'
  | 'UNSUPPORTED_VERSION'
  | 'CRYPTO_HASH_MISMATCH'
  | 'BLOCKCHAIN_INVALID'
  | 'INVALID_SALT_FORMAT';

export interface VerifyDegreeErrorResponse {
  verified: false;
  errorCode: VerificationErrorCode | string;
  message: string;
}

export type VerificationResultType =
  | { kind: 'valid'; data: VerifyDegreeSuccessResponse }
  | { kind: 'revoked'; data: VerifyDegreeSuccessResponse }
  | { kind: 'not_found'; errorCode: string; message: string }
  | { kind: 'tampered'; errorCode: VerificationErrorCode; message: string }
  | { kind: 'input_error'; errorCode: string; message: string }
  | { kind: 'server_error'; message: string }
  | { kind: 'rate_limited'; message: string };

export interface DegreeVersionItem {
  version: number;
  effectiveAt: string;
  isCurrent: boolean;
}

export interface DegreeVersionListResponse {
  degreeCode: string;
  currentVersion: number;
  versions: DegreeVersionItem[];
}

/** Degree code format: DEG-YYYY-NNNNNN (e.g., DEG-2026-000001) */
export const DEGREE_CODE_PATTERN = /^DEG-\d{4}-\d{6}$/;
