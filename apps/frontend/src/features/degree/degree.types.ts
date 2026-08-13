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

export interface DegreeListItem {
  id: string;
  degreeCode: string;
  studentId: string;
  studentName?: string;
  major: string;
  classification: string;
  status: DegreeStatus;
  issuedAt: string;
  createdAt: string;
}

export interface DegreeDetail {
  id: string;
  degreeCode: string;
  studentId: string;
  studentName?: string;
  major: string;
  classification: string;
  status: DegreeStatus;
  issuedAt: string;
  createdAt: string;
  txHash?: string;
  blockNumber?: number;
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
