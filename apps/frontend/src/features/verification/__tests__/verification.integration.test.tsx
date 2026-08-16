import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { VerificationPortalPage } from '../pages/VerificationPortalPage';
import { verificationApi } from '../verification.api';
import { HttpError } from '@/shared/api/http';
import type { VerifyDegreeSuccessResponse } from '../verification.types';

vi.mock('../verification.api', () => ({
  verificationApi: {
    verifyDegree: vi.fn(),
    getDegreeVersions: vi.fn(),
  },
}));

function renderPortal() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <VerificationPortalPage />
    </QueryClientProvider>,
  );
}

describe('VerificationPortalPage (Integration E2E)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const validResponse: VerifyDegreeSuccessResponse = {
    verified: true,
    status: 'Confirmed',
    verificationSource: 'Blockchain_Merkle_Root',
    degreeCode: 'DEG-2026-000001',
    version: 1,
    institutionName: 'Vietnam National University',
    studentFullName: 'Nguyen Van A',
    major: 'Information Technology',
    classification: 'Excellent',
    issuedAt: '2026-06-15T08:00:00Z',
    blockchain: {
      txHash: '0xabcdef1234567890abcdef1234567890abcdef12',
      blockNumber: 1001,
      merkleRoot: '0xroot',
      merkleProofJson: null,
    },
  };

  it('INT-01: Happy path verification for valid degree', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [{ version: 1, effectiveAt: '2026-06-15T08:00:00Z', isCurrent: true }],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockResolvedValue(validResponse);

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Degree Verified & Valid')).toBeInTheDocument();
      expect(screen.getByText('Vietnam National University')).toBeInTheDocument();
      expect(screen.getByText('Nguyen Van A')).toBeInTheDocument();
      expect(screen.getByText('Information Technology')).toBeInTheDocument();
      expect(screen.getByText('#1001')).toBeInTheDocument();
    });
  });

  it('INT-02: Revoked degree verification displays warning', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [{ version: 1, effectiveAt: '2026-06-15T08:00:00Z', isCurrent: true }],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
      ...validResponse,
      status: 'Revoked',
    });

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Degree Revoked')).toBeInTheDocument();
      expect(screen.getByText(/officially revoked and is no longer valid/i)).toBeInTheDocument();
    });
  });

  it('INT-03: Tampered data with hash mismatch displays pulsing warning', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockRejectedValue(
      new HttpError('validation', 422, 'Hash mismatch.', 'CRYPTO_HASH_MISMATCH'),
    );

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Integrity Verification Failed')).toBeInTheDocument();
      expect(screen.getByText(/Cryptographic Hash Mismatch/i)).toBeInTheDocument();
    });
  });

  it('INT-04: Blockchain invalid displays blockchain warning', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockRejectedValue(
      new HttpError('validation', 422, 'Blockchain invalid.', 'BLOCKCHAIN_INVALID'),
    );

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Integrity Verification Failed')).toBeInTheDocument();
      expect(screen.getByText(/Blockchain Ledger Validation Failed/i)).toBeInTheDocument();
    });
  });

  it('INT-05: Degree not found shows not-found result when submitted', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockRejectedValue(
      new HttpError('not_found', 404, 'Not found', 'DEGREE_NOT_FOUND'),
    );

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-999999');

    await waitFor(() => {
      expect(
        screen.getByText(/No degree found with this code. Please check and try again./i),
      ).toBeInTheDocument();
    });
  });

  it('INT-06: Server error shows error banner with retry option', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockRejectedValue(
      new HttpError('server_error', 500, 'Internal Server Error'),
    );

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Verification Request Failed')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
    });
  });

  it('INT-07: Network timeout shows connection error message', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockRejectedValue(
      new HttpError('timeout', null, 'Request timed out. Please check your connection and try again.'),
    );

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/Request timed out/i)).toBeInTheDocument();
    });
  });

  it('INT-08: Modifying degree code clears previous verification result', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockResolvedValue(validResponse);

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Degree Verified & Valid')).toBeInTheDocument();
    });

    // Type additional character -> result disappears
    await user.type(input, '2');

    await waitFor(() => {
      expect(screen.queryByText('Degree Verified & Valid')).not.toBeInTheDocument();
    });
  });

  it('INT-09: Sequential verifications for different degrees', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockResolvedValue(validResponse);

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Vietnam National University')).toBeInTheDocument();
    });

    // Clear and verify second degree
    const secondResponse = {
      ...validResponse,
      degreeCode: 'DEG-2026-000002',
      institutionName: 'Hanoi University of Science and Technology',
    };
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockResolvedValue(secondResponse);

    await user.clear(input);
    await user.type(input, 'DEG-2026-000002');
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Hanoi University of Science and Technology')).toBeInTheDocument();
    });
  });

  it('INT-10: Rate limit 429 response renders rate limit message', async () => {
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue({
      degreeCode: 'DEG-2026-000001',
      currentVersion: 1,
      versions: [],
    });
    (verificationApi.verifyDegree as ReturnType<typeof vi.fn>).mockRejectedValue(
      new HttpError('server_error', 429, 'Too many requests'),
    );

    const user = userEvent.setup();
    renderPortal();

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG-2026-000001');

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    await user.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText('Rate Limit Exceeded')).toBeInTheDocument();
    });
  });
});
