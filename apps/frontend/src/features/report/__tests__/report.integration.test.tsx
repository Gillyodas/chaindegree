import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReportFormModal } from '../components/ReportFormModal';
import { AdminReportsPage } from '../pages/AdminReportsPage';
import { reportApi } from '../report.api';
import { notification } from '@/shared/services/notification.service';
import { HttpError } from '@/shared/api/http';
import type { ReportListItem } from '../report.types';

vi.mock('../report.api', () => ({
  reportApi: {
    submitReport: vi.fn(),
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

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('Report Feature (Adversarial & E2E Integration Suite)', () => {
  const validDegreeId = '550e8400-e29b-41d4-a716-446655440000';
  const degreeCode = 'DEG-2026-000001';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('INT-01: Happy path - Student submits valid dispute report with PDF evidence', async () => {
    (reportApi.submitReport as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      reportId: 'rep-uuid-1234',
      degreeId: validDegreeId,
      status: 'Pending_Review',
      evidenceUrl: 'https://storage.chaindegree.io/evidences/evidence.pdf',
      createdAt: '2026-08-17T12:00:00Z',
    });

    const onSuccess = vi.fn();
    const onClose = vi.fn();
    const user = userEvent.setup();

    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={onClose}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
        onSuccess={onSuccess}
      />,
    );

    const descInput = screen.getByLabelText(/Detailed Description/i);
    await user.type(descInput, 'My graduation major is listed incorrectly as Math instead of Computer Science.');

    const file = new File(['%PDF-1.4 binary content'], 'transcript_official.pdf', {
      type: 'application/pdf',
    });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [file] } });

    const submitBtn = screen.getByRole('button', { name: /Submit Report/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(reportApi.submitReport).toHaveBeenCalled();
      expect(notification.success).toHaveBeenCalledWith(
        'Report submitted successfully. The system will review it as soon as possible.',
      );
      expect(onSuccess).toHaveBeenCalledWith('rep-uuid-1234');
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('INT-02: Adversarial - Attacker attempts to bypass upload limit with 6MB file', async () => {
    const user = userEvent.setup();
    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    const descInput = screen.getByLabelText(/Detailed Description/i);
    await user.type(descInput, 'Attempting to upload a large payload to exhaust server memory.');

    // 6MB file
    const largeFile = new File([new ArrayBuffer(6 * 1024 * 1024)], 'giant_bomb.pdf', {
      type: 'application/pdf',
    });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [largeFile] } });

    // Client-side dropzone catches file size immediately
    await waitFor(() => {
      expect(screen.getByText(/File size exceeds maximum allowed limit of 5MB/i)).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /Submit Report/i });
    await user.click(submitBtn);

    // API should NEVER be called
    expect(reportApi.submitReport).not.toHaveBeenCalled();
  });

  it('INT-03: Adversarial - Attacker uploads executable malware file (.exe)', async () => {
    const user = userEvent.setup();
    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    const descInput = screen.getByLabelText(/Detailed Description/i);
    await user.type(descInput, 'Attempting to inject binary payload disguised as evidence.');

    const exeFile = new File(['MZ payload'], 'malware.exe', {
      type: 'application/octet-stream',
    });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [exeFile] } });

    const submitBtn = screen.getByRole('button', { name: /Submit Report/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Only PDF, PNG, and JPG files are supported.')).toBeInTheDocument();
    });

    expect(reportApi.submitReport).not.toHaveBeenCalled();
  });

  it('INT-04: Adversarial - Server returns 409 Conflict (duplicate report under review)', async () => {
    const conflictError = new HttpError(
      'conflict',
      409,
      'A report for this degree is already under review by your account.',
      'Report.AlreadyExistsUnderReview',
    );
    (reportApi.submitReport as ReturnType<typeof vi.fn>).mockRejectedValueOnce(conflictError);

    const user = userEvent.setup();
    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    const descInput = screen.getByLabelText(/Detailed Description/i);
    await user.type(descInput, 'Duplicate submission on degree under current review.');

    const file = new File(['%PDF-1.4'], 'proof.pdf', { type: 'application/pdf' });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [file] } });

    const submitBtn = screen.getByRole('button', { name: /Submit Report/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.getByText('A report for this degree is already under review by your account.'),
      ).toBeInTheDocument();
    });
  });

  it('INT-05: Adversarial - Server returns 422 for corrupted file magic number', async () => {
    const validationError = new HttpError(
      'validation',
      422,
      'The evidence file signature or content type is invalid. Only valid PDF, PNG, or JPG files are allowed.',
      'Report.InvalidEvidenceFormat',
    );
    (reportApi.submitReport as ReturnType<typeof vi.fn>).mockRejectedValueOnce(validationError);

    const user = userEvent.setup();
    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    const descInput = screen.getByLabelText(/Detailed Description/i);
    await user.type(descInput, 'Submitting file with invalid internal magic bytes.');

    const fakePdf = new File(['CORRUPTED TEXT'], 'fake.pdf', { type: 'application/pdf' });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [fakePdf] } });

    const submitBtn = screen.getByRole('button', { name: /Submit Report/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.getByText(
          'The evidence file signature or content type is invalid. Only valid PDF, PNG, or JPG files are allowed.',
        ),
      ).toBeInTheDocument();
    });
  });

  it('INT-06: Adversarial - Network timeout during report upload does not crash UI', async () => {
    const timeoutError = new HttpError(
      'timeout',
      null,
      'Request timed out. Please check your connection and try again.',
    );
    (reportApi.submitReport as ReturnType<typeof vi.fn>).mockRejectedValueOnce(timeoutError);

    const user = userEvent.setup();
    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    const descInput = screen.getByLabelText(/Detailed Description/i);
    await user.type(descInput, 'Testing timeout resilience on slow networks.');

    const file = new File(['%PDF'], 'proof.pdf', { type: 'application/pdf' });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [file] } });

    const submitBtn = screen.getByRole('button', { name: /Submit Report/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.getByText('Request timed out. Please check your connection and try again.'),
      ).toBeInTheDocument();
    });
  });

  it('INT-07: End-to-end Admin Review & Approval Flow with Outbox / Reputation trigger', async () => {
    const mockReport: ReportListItem = {
      id: 'rep-int-001',
      degreeId: validDegreeId,
      degreeCode: degreeCode,
      reporterId: 'rec-007',
      reporterRole: 'Recruiter',
      reportType: 'Fraudulent_Data',
      description: 'Candidate presented a modified degree degree classification on job application.',
      status: 'Pending_Review',
      createdAt: '2026-08-17T09:00:00Z',
    };

    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce([mockReport]);
    (reportApi.approveReport as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      message: 'Report approved successfully.',
      reportId: 'rep-int-001',
      initiatedProcesses: ['DegreeRevocationChainTransaction', 'ReputationScoreRecalculationEvent'],
      timestamp: '2026-08-17T09:10:00Z',
    });

    const user = userEvent.setup();
    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText(degreeCode)).toBeInTheDocument();
      expect(screen.getByText('Fraudulent')).toBeInTheDocument();
    });

    const approveBtn = screen.getByRole('button', { name: /^Approve$/i });
    await user.click(approveBtn);

    // Confirm dialog appears
    await waitFor(() => {
      expect(screen.getByText('Approve Dispute / Fraud Report')).toBeInTheDocument();
    });

    const confirmApproveBtn = screen.getByRole('button', { name: /Confirm Approval/i });
    await user.click(confirmApproveBtn);

    await waitFor(() => {
      expect(reportApi.approveReport).toHaveBeenCalledWith('rep-int-001');
      expect(notification.success).toHaveBeenCalledWith(
        'Report approved successfully. Asynchronous revocation and reputation penalty processes have been initiated.',
      );
    });
  });

  it('INT-08: End-to-end Admin Review & Rejection Flow with justification reason', async () => {
    const mockReport: ReportListItem = {
      id: 'rep-int-002',
      degreeId: validDegreeId,
      degreeCode: degreeCode,
      reporterId: 'stud-008',
      reporterRole: 'Student',
      reportType: 'Administrative_Error',
      description: 'Requesting update on minor honorific label.',
      status: 'Pending_Review',
      createdAt: '2026-08-17T10:00:00Z',
    };

    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce([mockReport]);
    (reportApi.rejectReport as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      message: 'Report rejected.',
      reportId: 'rep-int-002',
      timestamp: '2026-08-17T10:05:00Z',
    });

    const user = userEvent.setup();
    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText(degreeCode)).toBeInTheDocument();
    });

    const rejectBtn = screen.getByRole('button', { name: /^Reject$/i });
    await user.click(rejectBtn);

    await waitFor(() => {
      expect(screen.getByText('Reject Report')).toBeInTheDocument();
    });

    const reasonInput = screen.getByLabelText(/Rejection Reason/i);
    await user.type(reasonInput, 'The requested change does not constitute an official degree record discrepancy.');

    const confirmRejectBtn = screen.getByRole('button', { name: /Confirm Rejection/i });
    await user.click(confirmRejectBtn);

    await waitFor(() => {
      expect(reportApi.rejectReport).toHaveBeenCalledWith(
        'rep-int-002',
        'The requested change does not constitute an official degree record discrepancy.',
      );
      expect(notification.success).toHaveBeenCalledWith('Report rejected successfully.');
    });
  });

  it('INT-09: End-to-end Physical Evidence Download via Blob Stream', async () => {
    const mockReport: ReportListItem = {
      id: 'rep-int-003',
      degreeId: validDegreeId,
      degreeCode: degreeCode,
      reporterId: 'rec-009',
      reporterRole: 'Recruiter',
      reportType: 'Administrative_Error',
      description: 'Discrepancy report with evidence',
      status: 'Pending_Review',
      createdAt: '2026-08-17T11:00:00Z',
    };

    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce([mockReport]);
    (reportApi.downloadReportEvidence as ReturnType<typeof vi.fn>).mockResolvedValueOnce(undefined);

    const user = userEvent.setup();
    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText(degreeCode)).toBeInTheDocument();
    });

    const evidenceBtn = screen.getByRole('button', { name: /Evidence/i });
    await user.click(evidenceBtn);

    await waitFor(() => {
      expect(reportApi.downloadReportEvidence).toHaveBeenCalledWith(
        'rep-int-003',
        'evidence-rep-int-003.pdf',
      );
    });
  });

  it('INT-10: Adversarial - XSS payload in Report description is safely escaped', async () => {
    const xssPayload = '<img src=x onerror=alert("XSS_ATTACK") /><script>window.hacked=true;</script>';
    const mockReport: ReportListItem = {
      id: 'rep-xss-001',
      degreeId: validDegreeId,
      degreeCode: degreeCode,
      reporterId: 'attacker-1',
      reporterRole: 'Student',
      reportType: 'Administrative_Error',
      description: xssPayload,
      status: 'Pending_Review',
      createdAt: '2026-08-17T12:00:00Z',
    };

    (reportApi.getReports as ReturnType<typeof vi.fn>).mockResolvedValueOnce([mockReport]);

    renderWithClient(<AdminReportsPage />);

    await waitFor(() => {
      expect(screen.getByText(xssPayload)).toBeInTheDocument();
    });

    // Verify script did not execute
    expect((window as unknown as { hacked?: boolean }).hacked).toBeUndefined();
  });
});
