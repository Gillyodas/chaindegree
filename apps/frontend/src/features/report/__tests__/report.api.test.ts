import { describe, it, expect, vi, beforeEach } from 'vitest';
import { reportApi } from '../report.api';
import { reportKeys } from '../report.keys';
import { httpClient } from '@/shared/api/http';

vi.mock('@/shared/api/http', () => ({
  httpClient: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

describe('reportKeys', () => {
  it('should generate correct query key hierarchy', () => {
    expect(reportKeys.all).toEqual(['reports']);
    expect(reportKeys.lists()).toEqual(['reports', 'list']);
    expect(reportKeys.detail('rep-123')).toEqual(['reports', 'detail', 'rep-123']);
    expect(reportKeys.evidence('rep-123')).toEqual(['reports', 'evidence', 'rep-123']);
  });
});

describe('reportApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('submitReport', () => {
    it('calls POST /api/v1/institutions/degrees/reports with multipart/form-data', async () => {
      const mockResponse = {
        reportId: 'rep-001',
        degreeId: 'deg-001',
        status: 'Pending_Review',
        evidenceUrl: 'https://storage.example.com/evidence.pdf',
        createdAt: '2026-08-17T00:00:00Z',
      };

      vi.mocked(httpClient.post).mockResolvedValueOnce({ data: mockResponse });

      const formData = new FormData();
      formData.append('degreeId', 'deg-001');

      const result = await reportApi.submitReport(formData);

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees/reports',
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('approveReport', () => {
    it('calls POST /api/v1/institutions/reports/{id}/approve', async () => {
      const mockResponse = {
        message: 'Report approved successfully.',
        reportId: 'rep-001',
        initiatedProcesses: ['DegreeRevocationChainTransaction'],
        timestamp: '2026-08-17T00:00:00Z',
      };

      vi.mocked(httpClient.post).mockResolvedValueOnce({ data: mockResponse });

      const result = await reportApi.approveReport('rep-001');

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/v1/institutions/reports/rep-001/approve',
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('rejectReport', () => {
    it('calls POST /api/v1/institutions/reports/{id}/reject with reason', async () => {
      const mockResponse = {
        message: 'Report rejected.',
        reportId: 'rep-001',
        timestamp: '2026-08-17T00:00:00Z',
      };

      vi.mocked(httpClient.post).mockResolvedValueOnce({ data: mockResponse });

      const result = await reportApi.rejectReport('rep-001', 'Insufficient proof provided');

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/v1/institutions/reports/rep-001/reject',
        { reason: 'Insufficient proof provided' },
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('downloadReportEvidence', () => {
    it('downloads evidence blob via object url', async () => {
      const mockBlob = new Blob(['sample-pdf-content'], { type: 'application/pdf' });
      vi.mocked(httpClient.get).mockResolvedValueOnce({
        data: mockBlob,
        headers: { 'content-type': 'application/pdf' },
      });

      const createObjectURLMock = vi.fn().mockReturnValue('blob:http://localhost/dummy');
      const revokeObjectURLMock = vi.fn();
      window.URL.createObjectURL = createObjectURLMock;
      window.URL.revokeObjectURL = revokeObjectURLMock;

      await reportApi.downloadReportEvidence('rep-001', 'custom-evidence.pdf');

      expect(httpClient.get).toHaveBeenCalledWith(
        '/api/v1/institutions/reports/rep-001/evidence',
        { responseType: 'blob' },
      );
      expect(createObjectURLMock).toHaveBeenCalled();
      expect(revokeObjectURLMock).toHaveBeenCalledWith('blob:http://localhost/dummy');
    });
  });

  describe('getReports', () => {
    it('returns report list when API succeeds', async () => {
      const mockList = [
        {
          id: 'rep-001',
          degreeId: 'deg-001',
          reporterId: 'stu-001',
          reporterRole: 'Student',
          reportType: 'Administrative_Error' as const,
          description: 'Spelling error in student name',
          status: 'Pending_Review' as const,
          createdAt: '2026-08-17T00:00:00Z',
        },
      ];

      vi.mocked(httpClient.get).mockResolvedValueOnce({ data: mockList });

      const result = await reportApi.getReports();
      expect(httpClient.get).toHaveBeenCalledWith('/api/v1/institutions/reports');
      expect(result).toEqual(mockList);
    });

    it('returns empty array when API fails gracefully', async () => {
      vi.mocked(httpClient.get).mockRejectedValueOnce(new Error('404 Not Found'));

      const result = await reportApi.getReports();
      expect(result).toEqual([]);
    });
  });
});
