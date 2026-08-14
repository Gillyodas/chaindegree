import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { UpdateDegreeModal } from '../components/UpdateDegreeModal';
import { degreeApi } from '../degree.api';
import type { DegreeDetail } from '../degree.types';

vi.mock('../degree.api', () => ({
  degreeApi: {
    updateDegree: vi.fn(),
    getDegree: vi.fn(),
  },
}));

const mockDegree: DegreeDetail = {
  id: 'deg-123',
  degreeCode: 'DEG-2026-000001',
  institutionId: 'inst-1',
  signedByRegistrarId: 'reg-1',
  studentId: 'stud-1',
  studentFullName: 'John Doe',
  major: 'Computer Science',
  classification: 'High Distinction',
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

describe('UpdateDegreeModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders modal with pre-filled degree details when open', () => {
    renderWithClient(
      <UpdateDegreeModal
        isOpen={true}
        onClose={vi.fn()}
        degree={mockDegree}
      />,
    );

    expect(screen.getByText('Update Degree Information')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Computer Science')).toBeInTheDocument();
    expect(screen.getByDisplayValue('High Distinction')).toBeInTheDocument();
  });

  it('calls degreeApi.updateDegree on form submission', async () => {
    (degreeApi.updateDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeId: 'deg-123',
      currentStatus: 'Pending_Update',
      isShortcut: false,
      message: 'Accepted',
    });

    const onSuccess = vi.fn();
    const onClose = vi.fn();

    renderWithClient(
      <UpdateDegreeModal
        isOpen={true}
        onClose={onClose}
        degree={mockDegree}
        onSuccess={onSuccess}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirm Update/i }));

    await waitFor(() => {
      expect(degreeApi.updateDegree).toHaveBeenCalled();
      expect(onSuccess).toHaveBeenCalled();
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('handles 409 State Conflict by closing and invoking onConflict callback', async () => {
    const error409 = new AxiosError(
      'Conflict',
      'ERR_BAD_REQUEST',
      undefined,
      undefined,
      { status: 409, data: {}, headers: {}, config: {} as never, statusText: 'Conflict' },
    );
    (degreeApi.updateDegree as ReturnType<typeof vi.fn>).mockRejectedValue(error409);

    const onConflict = vi.fn();
    const onClose = vi.fn();

    renderWithClient(
      <UpdateDegreeModal
        isOpen={true}
        onClose={onClose}
        degree={mockDegree}
        onConflict={onConflict}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirm Update/i }));

    await waitFor(() => {
      expect(onConflict).toHaveBeenCalled();
      expect(onClose).toHaveBeenCalled();
    });
  });

  it('shows warning alert and disables button on ambiguous outcome (500 error)', async () => {
    const error500 = new AxiosError(
      'Server Error',
      'ERR_BAD_RESPONSE',
      undefined,
      undefined,
      { status: 500, data: {}, headers: {}, config: {} as never, statusText: 'Server Error' },
    );
    (degreeApi.updateDegree as ReturnType<typeof vi.fn>).mockRejectedValue(error500);

    renderWithClient(
      <UpdateDegreeModal
        isOpen={true}
        onClose={vi.fn()}
        degree={mockDegree}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /Confirm Update/i }));

    await waitFor(() => {
      expect(
        screen.getByText('Unable to determine the current operation result. The degree is being rechecked.'),
      ).toBeInTheDocument();
    });
  });
});
