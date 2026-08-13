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
