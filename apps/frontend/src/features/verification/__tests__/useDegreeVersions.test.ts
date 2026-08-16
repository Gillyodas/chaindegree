import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useDegreeVersions } from '../hooks/useDegreeVersions';
import { verificationApi } from '../verification.api';
import { HttpError } from '@/shared/api/http';

vi.mock('../verification.api', () => ({
  verificationApi: {
    getDegreeVersions: vi.fn(),
  },
}));

function createTestWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: queryClient }, children);
  return { queryClient, wrapper };
}

describe('useDegreeVersions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('does NOT call getDegreeVersions when degreeCode does not match pattern', () => {
    const { wrapper } = createTestWrapper();
    const { result } = renderHook(() => useDegreeVersions('DEG-123'), { wrapper });

    act(() => {
      vi.advanceTimersByTime(600);
    });

    expect(result.current.isValidPattern).toBe(false);
    expect(verificationApi.getDegreeVersions).not.toHaveBeenCalled();
    expect(result.current.versions).toEqual([]);
  });

  it('calls getDegreeVersions when degreeCode matches pattern after debounce', async () => {
    const mockVersions = {
      degreeCode: 'DEG-2026-000001',
      currentVersion: 2,
      versions: [
        { version: 2, effectiveAt: '2026-07-01T00:00:00Z', isCurrent: true },
        { version: 1, effectiveAt: '2026-06-01T00:00:00Z', isCurrent: false },
      ],
    };
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockResolvedValue(mockVersions);

    const { wrapper } = createTestWrapper();
    const { result } = renderHook(() => useDegreeVersions('DEG-2026-000001'), { wrapper });

    act(() => {
      vi.advanceTimersByTime(600);
    });

    await act(async () => {
      vi.useRealTimers();
      await waitFor(() => expect(result.current.isSuccess).toBe(true));
    });

    expect(verificationApi.getDegreeVersions).toHaveBeenCalledWith(
      'DEG-2026-000001',
      expect.any(Object),
    );
    expect(result.current.versions).toEqual(mockVersions.versions);
    expect(result.current.currentVersion).toBe(2);
    expect(result.current.degreeNotFound).toBe(false);
  });

  it('sets degreeNotFound: true when API returns 404 DEGREE_NOT_FOUND', async () => {
    const notFoundError = new HttpError(
      'not_found',
      404,
      'No degree found with the specified code.',
      'DEGREE_NOT_FOUND',
    );
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockRejectedValue(notFoundError);

    const { wrapper } = createTestWrapper();
    const { result } = renderHook(() => useDegreeVersions('DEG-2026-999999'), { wrapper });

    act(() => {
      vi.advanceTimersByTime(600);
    });

    await act(async () => {
      vi.useRealTimers();
      await waitFor(() => expect(result.current.degreeNotFound).toBe(true));
    });

    expect(result.current.degreeNotFound).toBe(true);
    expect(result.current.versions).toEqual([]);
  });

  it('handles server 500 error gracefully without crashing', async () => {
    const serverError = new HttpError(
      'server_error',
      500,
      'Internal server error',
    );
    (verificationApi.getDegreeVersions as ReturnType<typeof vi.fn>).mockRejectedValue(serverError);

    const { wrapper } = createTestWrapper();
    const { result } = renderHook(() => useDegreeVersions('DEG-2026-000001'), { wrapper });

    act(() => {
      vi.advanceTimersByTime(600);
    });

    await act(async () => {
      vi.useRealTimers();
      await waitFor(() => expect(result.current.error).toBeTruthy());
    });

    expect(result.current.degreeNotFound).toBe(false);
    expect(result.current.versions).toEqual([]);
  });
});
