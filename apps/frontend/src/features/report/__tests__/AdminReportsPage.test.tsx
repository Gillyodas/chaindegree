import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AdminReportsPage } from '../pages/AdminReportsPage';
import { reportApi } from '../report.api';
import type { ReportListItem } from '../report.types';

vi.mock('../report.api', () => ({
  reportApi: {
    getReports: vi.fn(),
    approveReport: vi.fn(),
    rejectReport: vi.fn(),
    downloadReportEvidence: vi.fn(),
  },
}));

vi.mock('@/shared/services/notification.service', () => ({
  notification: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

const mockReports: ReportListItem[] = [
  {
    id: 'rep-001-uuid-1111',
    degreeId: 'deg-001',
    degreeCode: 'DEG-2026-000001',
    reporterId: 'stud-1',
    reporterRole: 'Student',
    reportType: 'Administrative_Error',
    description: 'Minor typo in student name',
    status: 'Pending_Review',
    createdAt: '2026-08-17T00:00:00Z',
  },
  {
    id: 'rep-002-uuid-2222',
    degreeId: 'deg-002',
    degreeCode: 'DEG-2026-000002',
    reporterId: 'rec-1',
    reporterRole: 'Recruiter',
    reportType: 'Fraudulent_Data',
    description: 'Fabricated transcript and unauthorized degree seal',
    status: 'Approved',
    createdAt: '2026-08-16T00:00:00Z',
  },
];

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('AdminReportsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading spinner when fetching reports', () => {
    (reportApi.getReports as ReturnType<typeof vi.fn>).mockImplementation(
      () => new Promise(() => {}),
    );

    renderWithClient(<AdminReportsPage />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('renders empty state when there are no reports', async () => {
    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce([]);

    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText('No reports available')).toBeInTheDocument();
    });
  });

  it('renders reports table with data, badges and action buttons', async () => {
    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce(mockReports);

    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText('Report Management')).toBeInTheDocument();
      expect(screen.getByText('DEG-2026-000001')).toBeInTheDocument();
      expect(screen.getByText('DEG-2026-000002')).toBeInTheDocument();
      expect(screen.getByText('Administrative')).toBeInTheDocument();
      expect(screen.getByText('Fraudulent')).toBeInTheDocument();
    });

    // Pending report should have Approve and Reject buttons
    expect(screen.getByRole('button', { name: /^Approve$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^Reject$/i })).toBeInTheDocument();
  });

  it('handles approve report flow with confirmation dialog', async () => {
    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce(mockReports);
    (reportApi.approveReport as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      message: 'Approved',
      reportId: 'rep-001-uuid-1111',
      initiatedProcesses: [],
      timestamp: '2026-08-17T00:00:00Z',
    });

    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText('DEG-2026-000001')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /^Approve$/i }));

    // Confirmation dialog appears
    await waitFor(() => {
      expect(screen.getByText('Approve Dispute / Fraud Report')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /Confirm Approval/i }));

    await waitFor(() => {
      expect(reportApi.approveReport).toHaveBeenCalledWith('rep-001-uuid-1111');
    });
  });

  it('handles reject report flow with rejection modal', async () => {
    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce(mockReports);
    (reportApi.rejectReport as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      message: 'Rejected',
      reportId: 'rep-001-uuid-1111',
      timestamp: '2026-08-17T00:00:00Z',
    });

    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText('DEG-2026-000001')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /^Reject$/i }));

    await waitFor(() => {
      expect(screen.getByText('Reject Report')).toBeInTheDocument();
    });

    const reasonInput = screen.getByLabelText(/Rejection Reason/i);
    fireEvent.change(reasonInput, {
      target: { value: 'Evidence does not prove any discrepancy.' },
    });

    fireEvent.click(screen.getByRole('button', { name: /Confirm Rejection/i }));

    await waitFor(() => {
      expect(reportApi.rejectReport).toHaveBeenCalledWith(
        'rep-001-uuid-1111',
        'Evidence does not prove any discrepancy.',
      );
    });
  });

  it('triggers evidence download when Evidence button is clicked', async () => {
    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce(mockReports);
    (reportApi.downloadReportEvidence as ReturnType<typeof vi.fn>).mockResolvedValueOnce(undefined);

    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText('DEG-2026-000001')).toBeInTheDocument();
    });

    const evidenceButtons = screen.getAllByRole('button', { name: /Evidence/i });
    fireEvent.click(evidenceButtons[0]);

    await waitFor(() => {
      expect(reportApi.downloadReportEvidence).toHaveBeenCalledWith(
        'rep-001-uuid-1111',
        'evidence-rep-001-uuid-1111.pdf',
      );
    });
  });
});
