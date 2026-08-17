import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReportFormModal } from '../components/ReportFormModal';
import { reportApi } from '../report.api';

vi.mock('../report.api', () => ({
  reportApi: {
    submitReport: vi.fn(),
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
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('ReportFormModal', () => {
  const validDegreeId = '550e8400-e29b-41d4-a716-446655440000';
  const degreeCode = 'DEG-2026-000001';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders modal with degree code when open', () => {
    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    expect(screen.getByText('Report Degree Issue / Fraud')).toBeInTheDocument();
    expect(screen.getByText(degreeCode)).toBeInTheDocument();
    expect(screen.getByLabelText(/Report Type/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Detailed Description/i)).toBeInTheDocument();
  });

  it('validates required fields before submitting', async () => {
    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Submit Report/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/Description must be at least 10 characters long/i),
      ).toBeInTheDocument();
      expect(screen.getByText(/An evidence file is required/i)).toBeInTheDocument();
    });

    expect(reportApi.submitReport).not.toHaveBeenCalled();
  });

  it('submits form with valid input and file attachment', async () => {
    (reportApi.submitReport as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      reportId: 'rep-999',
      degreeId: validDegreeId,
      status: 'Pending_Review',
      evidenceUrl: 'https://example.com/evidence.pdf',
      createdAt: '2026-08-17T00:00:00Z',
    });

    const onSuccess = vi.fn();
    const onClose = vi.fn();

    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={onClose}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
        onSuccess={onSuccess}
      />,
    );

    // Enter description
    const descInput = screen.getByLabelText(/Detailed Description/i);
    fireEvent.change(descInput, {
      target: { value: 'This degree has an incorrect graduation classification listed.' },
    });

    // Upload valid file (Dialog renders in document.body)
    const file = new File(['valid pdf content'], 'evidence.pdf', {
      type: 'application/pdf',
    });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    expect(fileInput).not.toBeNull();
    fireEvent.change(fileInput, { target: { files: [file] } });

    // Click submit
    fireEvent.click(screen.getByRole('button', { name: /Submit Report/i }));

    await waitFor(() => {
      expect(reportApi.submitReport).toHaveBeenCalled();
      expect(onSuccess).toHaveBeenCalledWith('rep-999');
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('displays server error alert when API rejects submission', async () => {
    (reportApi.submitReport as ReturnType<typeof vi.fn>).mockRejectedValueOnce(
      new Error('A report for this degree is already under review by your account.'),
    );

    renderWithClient(
      <ReportFormModal
        isOpen={true}
        onClose={vi.fn()}
        degreeId={validDegreeId}
        degreeCode={degreeCode}
      />,
    );

    const descInput = screen.getByLabelText(/Detailed Description/i);
    fireEvent.change(descInput, {
      target: { value: 'This degree has an incorrect graduation classification listed.' },
    });

    const file = new File(['valid pdf content'], 'evidence.pdf', {
      type: 'application/pdf',
    });
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    expect(fileInput).not.toBeNull();
    fireEvent.change(fileInput, { target: { files: [file] } });

    fireEvent.click(screen.getByRole('button', { name: /Submit Report/i }));

    await waitFor(() => {
      expect(
        screen.getByText('A report for this degree is already under review by your account.'),
      ).toBeInTheDocument();
    });
  });
});
