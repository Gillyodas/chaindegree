export type ApiError = {
  errorCode?: string;
  message: string;
  details?: Record<string, string[]>;
};

export type PaginatedResponse<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
};

export type DegreeStatus =
  | 'Pending_Confirmation'
  | 'Confirmed'
  | 'Confirmation_Error'
  | 'Pending_Update'
  | 'Pending_Revocation'
  | 'Revoked'
  | 'Frozen';

export type ReportStatus = 'Pending_Review' | 'Approved' | 'Rejected';

export type RankStatus = 'Highly_Qualified' | 'Under_Qualified';

export type UserRole = 'Registrar' | 'Student' | 'Recruiter' | 'Admin';
