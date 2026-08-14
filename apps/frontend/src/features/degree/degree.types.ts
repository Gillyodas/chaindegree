export type DegreeStatus =
  | 'Pending_Confirmation'
  | 'Confirmed'
  | 'Confirmation_Error'
  | 'Pending_Update'
  | 'Pending_Revocation'
  | 'Revoked'
  | 'Frozen';

export interface IssueDegreeItemRequest {
  studentId: string;
  major: string;
  classification: string;
  issuedAt: string;
}

export interface IssueDegreeRequest {
  degrees: IssueDegreeItemRequest[];
}

export interface IssueDegreeFailure {
  studentId: string;
  major: string;
  reason: string;
}

export interface IssueDegreeResponse {
  message: string;
  acceptedCount: number;
  degreeIds: string[];
  failures: IssueDegreeFailure[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface DegreeListItem {
  id: string;
  degreeCode: string;
  studentId: string;
  studentFullName: string;
  major: string;
  classification: string;
  status: DegreeStatus | string;
  issuedAt: string;
  txHashBlockchain?: string | null;
}

export interface DegreeDetail {
  id: string;
  degreeCode: string;
  institutionId: string;
  signedByRegistrarId: string;
  studentId: string;
  studentFullName: string;
  major: string;
  classification: string;
  status: DegreeStatus | string;
  issuedAt: string;
  txHashBlockchain?: string | null;
  currentVersion: number;
  createdAt: string;
  updatedAt?: string | null;
}

export interface BatchStatusResponse {
  batchId: string;
  status: string;
  totalDegrees: number;
  processedDegrees: number;
  failedDegrees: number;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateDegreeRequest {
  major: string;
  classification: string;
  reasonCode: string;
}

export interface UpdateDegreeResponse {
  degreeId: string;
  currentStatus: DegreeStatus | string;
  isShortcut: boolean;
  message: string;
}

export interface RevokeDegreeRequest {
  reasonCode: string;
}

export interface RevokeDegreeResponse {
  degreeId: string;
  currentStatus: DegreeStatus | string;
  isShortcut: boolean;
  message: string;
  reputationImpact?: string;
}

export interface DegreeActionReasonOption {
  code: string;
  description: string;
}

export const DEGREE_REVOCATION_REASONS: DegreeActionReasonOption[] = [
  { code: 'R-01', description: 'R-01: Fraudulent Data - Academic credentials forgery' },
  { code: 'R-02', description: 'R-02: Fraudulent Data - Forged identity' },
  { code: 'S-02', description: 'S-02: Administrative Error - System entry duplicate' },
  { code: 'H-01', description: 'H-01: System Compromise / Hack' },
  { code: 'S-01', description: 'S-01: Administrative Error - Incorrect name/classification' },
];

export const DEGREE_UPDATE_REASONS: DegreeActionReasonOption[] = [
  { code: 'S-01', description: 'S-01: Administrative Error - Incorrect name/classification' },
  { code: 'S-02', description: 'S-02: Administrative Error - System entry duplicate' },
];
