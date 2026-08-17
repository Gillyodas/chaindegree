import type { ReportStatus } from '@/shared/types/api.types';

export type ReportType = 'Administrative_Error' | 'Fraudulent_Data';

export interface SubmitReportRequest {
  degreeId: string;
  reportType: ReportType;
  description: string;
  evidenceFile: File;
}

export interface SubmitReportResponse {
  reportId: string;
  degreeId: string;
  status: ReportStatus;
  evidenceUrl: string;
  createdAt: string;
}

export interface ApproveReportResponse {
  message: string;
  reportId: string;
  initiatedProcesses: string[];
  timestamp: string;
}

export interface RejectReportResponse {
  message: string;
  reportId: string;
  timestamp: string;
}

export interface ReportListItem {
  id: string;
  degreeId: string;
  degreeCode?: string;
  reporterId: string;
  reporterRole: string;
  reportType: ReportType;
  description: string;
  status: ReportStatus;
  evidenceUrl?: string;
  createdAt: string;
}
