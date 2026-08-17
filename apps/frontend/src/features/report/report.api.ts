import { httpClient } from '@/shared/api/http';
import type {
  SubmitReportResponse,
  ApproveReportResponse,
  RejectReportResponse,
  ReportListItem,
} from './report.types';

export const reportApi = {
  submitReport: async (formData: FormData): Promise<SubmitReportResponse> => {
    const response = await httpClient.post<SubmitReportResponse>(
      '/api/v1/institutions/degrees/reports',
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      },
    );
    return response.data;
  },

  approveReport: async (id: string): Promise<ApproveReportResponse> => {
    const response = await httpClient.post<ApproveReportResponse>(
      `/api/v1/institutions/reports/${id}/approve`,
    );
    return response.data;
  },

  rejectReport: async (id: string, reason: string): Promise<RejectReportResponse> => {
    const response = await httpClient.post<RejectReportResponse>(
      `/api/v1/institutions/reports/${id}/reject`,
      { reason },
    );
    return response.data;
  },

  downloadReportEvidence: async (id: string, fileName: string = `evidence-${id}.pdf`): Promise<void> => {
    const response = await httpClient.get<Blob>(
      `/api/v1/institutions/reports/${id}/evidence`,
      {
        responseType: 'blob',
      },
    );

    const contentTypeHeader = response.headers['content-type'];
    const contentType =
      typeof contentTypeHeader === 'string'
        ? contentTypeHeader
        : 'application/octet-stream';

    const blob = new Blob([response.data], {
      type: contentType,
    });
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
  },

  getReports: async (): Promise<ReportListItem[]> => {
    try {
      const response = await httpClient.get<ReportListItem[]>(
        '/api/v1/institutions/reports',
      );
      return response.data || [];
    } catch {
      // Graceful fallback if endpoint is not ready or returns 404
      return [];
    }
  },
};
