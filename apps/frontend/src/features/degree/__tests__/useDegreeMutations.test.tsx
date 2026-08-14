import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { AxiosError } from 'axios';
import { useUpdateDegreeMutation, useRevokeDegreeMutation } from '../hooks/useDegreeMutations';
import { degreeApi } from '../degree.api';

vi.mock('../degree.api', () => ({
  degreeApi: {
    updateDegree: vi.fn(),
    revokeDegree: vi.fn(),
    getDegree: vi.fn(),
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

describe('useDegreeMutations', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useUpdateDegreeMutation', () => {
    it('executes updateDegree and invalidates degree queries on success', async () => {
      (degreeApi.updateDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
        degreeId: 'deg-1',
        currentStatus: 'Pending_Update',
        isShortcut: false,
        message: 'Accepted',
      });
      (degreeApi.getDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
        id: 'deg-1',
        status: 'Pending_Update',
      });

      const { queryClient, wrapper } = createTestWrapper();
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const { result } = renderHook(() => useUpdateDegreeMutation(), { wrapper });

      await result.current.mutateAsync({
        id: 'deg-1',
        data: { major: 'CS', classification: 'Good', reasonCode: 'CORRECTION' },
        idempotencyKey: 'key-1',
      });

      expect(degreeApi.updateDegree).toHaveBeenCalledWith(
        'deg-1',
        { major: 'CS', classification: 'Good', reasonCode: 'CORRECTION' },
        'key-1',
      );
      expect(invalidateSpy).toHaveBeenCalled();
    });

    it('triggers reconciliation GET query on ambiguous outcome (500 error)', async () => {
      const error500 = new AxiosError('Server error', '500', undefined, undefined, {
        status: 500,
        data: {},
        headers: {},
        config: {} as never,
        statusText: 'Server Error',
      });
      (degreeApi.updateDegree as ReturnType<typeof vi.fn>).mockRejectedValue(error500);
      (degreeApi.getDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
        id: 'deg-1',
        status: 'Pending_Update',
      });

      const { wrapper } = createTestWrapper();
      const { result } = renderHook(() => useUpdateDegreeMutation(), { wrapper });

      try {
        await result.current.mutateAsync({
          id: 'deg-1',
          data: { major: 'CS', classification: 'Good', reasonCode: 'CORRECTION' },
          idempotencyKey: 'key-2',
        });
      } catch {
        // Expected mutation failure
      }

      await waitFor(() => {
        expect(degreeApi.getDegree).toHaveBeenCalledWith('deg-1');
      });
    });
  });

  describe('useRevokeDegreeMutation', () => {
    it('executes revokeDegree and invalidates degree queries on success', async () => {
      (degreeApi.revokeDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
        degreeId: 'deg-2',
        currentStatus: 'Revoked',
        isShortcut: true,
        message: 'Revoked',
      });
      (degreeApi.getDegree as ReturnType<typeof vi.fn>).mockResolvedValue({
        id: 'deg-2',
        status: 'Revoked',
      });

      const { queryClient, wrapper } = createTestWrapper();
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      const { result } = renderHook(() => useRevokeDegreeMutation(), { wrapper });

      await result.current.mutateAsync({
        id: 'deg-2',
        data: { reasonCode: 'ACADEMIC_FRAUD' },
        idempotencyKey: 'key-3',
      });

      expect(degreeApi.revokeDegree).toHaveBeenCalledWith(
        'deg-2',
        { reasonCode: 'ACADEMIC_FRAUD' },
        'key-3',
      );
      expect(invalidateSpy).toHaveBeenCalled();
    });
  });
});
