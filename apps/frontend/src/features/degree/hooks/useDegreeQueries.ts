import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { degreeApi } from '../degree.api';
import { degreeKeys } from '../degree.keys';
import type {
  DegreeListItem,
  DegreeDetail,
  BatchStatusResponse,
  PagedResult,
} from '../degree.types';

export interface UseDegreesQueryOptions {
  pageIndex?: number;
  pageSize?: number;
  enabled?: boolean;
  isSignalRConnected?: boolean;
}

export function useDegreesQuery(options: UseDegreesQueryOptions = {}) {
  const { pageIndex = 1, pageSize = 20, enabled = true, isSignalRConnected = false } = options;

  return useQuery<PagedResult<DegreeListItem>, Error>({
    queryKey: degreeKeys.lists(pageIndex, pageSize),
    queryFn: () => degreeApi.getDegrees(pageIndex, pageSize),
    enabled,
    refetchInterval: isSignalRConnected ? false : 5000,
  });
}

export function useDegreeDetailQuery(id: string) {
  return useQuery<DegreeDetail, Error>({
    queryKey: degreeKeys.detail(id),
    queryFn: () => degreeApi.getDegree(id),
    enabled: Boolean(id),
  });
}

export function useBatchStatusQuery(batchId: string) {
  return useQuery<BatchStatusResponse, Error>({
    queryKey: degreeKeys.batchStatus(batchId),
    queryFn: () => degreeApi.getBatchStatus(batchId),
    enabled: Boolean(batchId),
  });
}

export function useRetryDegreeMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: (id: string) => degreeApi.retryDegreeConfirmation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: degreeKeys.all });
    },
  });
}
