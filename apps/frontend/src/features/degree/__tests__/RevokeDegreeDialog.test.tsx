import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { RevokeDegreeDialog } from '../components/RevokeDegreeDialog';
import { degreeApi } from '../degree.api';
import type { DegreeDetail } from '../degree.types';

vi.mock('../degree.api', () => ({
  degreeApi: {
    revokeDegree: vi.fn(),
    getDegree: vi.fn(),
  },
}));

const mockDegree: DegreeDetail = {
  id: 'deg-456',
  degreeCode: 'DEG-2026-000002',
  institutionId: 'inst-1',
  signedByRegistrarId: 'reg-1',
  studentId: 'stud-2',
  studentFullName: 'Jane Smith',
  major: 'Software Engineering',
  classification: 'Merit',
  status: 'Confirmed',
  issuedAt: '2026-01-01T00:00:00Z',
  currentVersion: 1,
  createdAt: '2026-01-01T00:00:00Z',
};

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('RevokeDegreeDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders dialog with degree code warning when open', () => {
    renderWithClient(
      <RevokeDegreeDialog
        isOpen={true}
        onClose={vi.fn()}
        degree={mockDegree}
      />,
    );

    expect(screen.getByText('Revoke Degree')).toBeInTheDocument();
    expect(screen.getByText('DEG-2026-000002')).toBeInTheDocument();
    expect(screen.getByDisplayValue(/R-01/i)).toBeInTheDocument();
  });

  it('submits revocation request and passes async processing message', async () => {
    (degreeApi.revokeDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeId: 'deg-456',
      currentStatus: 'Pending_Revocation',
      isShortcut: false,
      message: 'Accepted',
    });

    const onSuccess = vi.fn();
    const onClose = vi.fn();

    renderWithClient(
      <RevokeDegreeDialog
        isOpen={true}
        onClose={onClose}
        degree={mockDegree}
        onSuccess={onSuccess}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirm Revocation/i }));

    await waitFor(() => {
      expect(degreeApi.revokeDegree).toHaveBeenCalled();
      expect(onSuccess).toHaveBeenCalledWith(
        'Revocation request accepted. Processing continues in the background.',
      );
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('passes direct revocation message when response is shortcut / Revoked', async () => {
    (degreeApi.revokeDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeId: 'deg-456',
      currentStatus: 'Revoked',
      isShortcut: true,
      message: 'Revoked',
    });

    const onSuccess = vi.fn();

    renderWithClient(
      <RevokeDegreeDialog
        isOpen={true}
        onClose={vi.fn()}
        degree={mockDegree}
        onSuccess={onSuccess}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirm Revocation/i }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledWith('Degree revoked successfully.');
    });
  });

  it('handles 409 State Conflict by closing and triggering onConflict', async () => {
    const error409 = new AxiosError(
      'Conflict',
      'ERR_BAD_REQUEST',
      undefined,
      undefined,
      { status: 409, data: {}, headers: {}, config: {} as never, statusText: 'Conflict' },
    );
    (degreeApi.revokeDegree as ReturnType<typeof vi.fn>).mockRejectedValue(error409);

    const onConflict = vi.fn();
    const onClose = vi.fn();

    renderWithClient(
      <RevokeDegreeDialog
        isOpen={true}
        onClose={onClose}
        degree={mockDegree}
        onConflict={onConflict}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirm Revocation/i }));

    await waitFor(() => {
      expect(onConflict).toHaveBeenCalled();
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('displays ambiguous outcome warning on 503 Service Unavailable', async () => {
    const error503 = new AxiosError(
      'Unavailable',
      'ERR_BAD_RESPONSE',
      undefined,
      undefined,
      { status: 503, data: {}, headers: {}, config: {} as never, statusText: 'Service Unavailable' },
    );
    (degreeApi.revokeDegree as ReturnType<typeof vi.fn>).mockRejectedValue(error503);

    renderWithClient(
      <RevokeDegreeDialog
        isOpen={true}
        onClose={vi.fn()}
        degree={mockDegree}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirm Revocation/i }));

    await waitFor(() => {
      expect(
        screen.getByText('Unable to determine the current operation result. The degree is being rechecked.'),
      ).toBeInTheDocument();
    });
  });
});
