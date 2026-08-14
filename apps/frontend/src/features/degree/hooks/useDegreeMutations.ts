import { useMutation, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { degreeApi } from '../degree.api';
import { degreeKeys } from '../degree.keys';
import type {
  UpdateDegreeRequest,
  UpdateDegreeResponse,
  RevokeDegreeRequest,
  RevokeDegreeResponse,
  DegreeDetail,
} from '../degree.types';

export interface UpdateDegreeMutationParams {
  id: string;
  data: UpdateDegreeRequest;
  idempotencyKey: string;
}

export interface RevokeDegreeMutationParams {
  id: string;
  data: RevokeDegreeRequest;
  idempotencyKey: string;
}

export interface MutationMetaResult<T> {
  data?: T;
  reconciledDegree?: DegreeDetail | null;
  isConflict?: boolean;
  isAmbiguous?: boolean;
  isSyncFailed?: boolean;
  errorMessage?: string;
}

export function useUpdateDegreeMutation() {
  const queryClient = useQueryClient();

  return useMutation<UpdateDegreeResponse, AxiosError | Error, UpdateDegreeMutationParams>({
    mutationFn: ({ id, data, idempotencyKey }) =>
      degreeApi.updateDegree(id, data, idempotencyKey),
    onSuccess: async (_data, variables) => {
      // Invalidate and await canonical refetch to ensure UI synchronization
      await queryClient.invalidateQueries({ queryKey: degreeKeys.all });
      try {
        await queryClient.refetchQueries({ queryKey: degreeKeys.detail(variables.id) });
      } catch {
        // Sync failure handling handled by component if needed
      }
    },
    onError: async (error, variables) => {
      const isAxiosError = error instanceof AxiosError;
      const status = isAxiosError ? error.response?.status : undefined;

      if (status === 409) {
        // State conflict: refetch canonical state immediately
        await queryClient.invalidateQueries({ queryKey: degreeKeys.all });
        await queryClient.refetchQueries({ queryKey: degreeKeys.detail(variables.id) });
      } else if (!status || status >= 500) {
        // Ambiguous outcome (Network lost, Timeout, 500/503) -> Reconciliation attempt
        try {
          await queryClient.fetchQuery({
            queryKey: degreeKeys.detail(variables.id),
            queryFn: () => degreeApi.getDegree(variables.id),
          });
        } catch {
          // Canonical refetch failed during reconciliation
        }
      }
    },
  });
}

export function useRevokeDegreeMutation() {
  const queryClient = useQueryClient();

  return useMutation<RevokeDegreeResponse, AxiosError | Error, RevokeDegreeMutationParams>({
    mutationFn: ({ id, data, idempotencyKey }) =>
      degreeApi.revokeDegree(id, data, idempotencyKey),
    onSuccess: async (_data, variables) => {
      await queryClient.invalidateQueries({ queryKey: degreeKeys.all });
      try {
        await queryClient.refetchQueries({ queryKey: degreeKeys.detail(variables.id) });
      } catch {
        // Sync failure handling
      }
    },
    onError: async (error, variables) => {
      const isAxiosError = error instanceof AxiosError;
      const status = isAxiosError ? error.response?.status : undefined;

      if (status === 409) {
        await queryClient.invalidateQueries({ queryKey: degreeKeys.all });
        await queryClient.refetchQueries({ queryKey: degreeKeys.detail(variables.id) });
      } else if (!status || status >= 500) {
        try {
          await queryClient.fetchQuery({
            queryKey: degreeKeys.detail(variables.id),
            queryFn: () => degreeApi.getDegree(variables.id),
          });
        } catch {
          // Refetch failed
        }
      }
    },
  });
}
