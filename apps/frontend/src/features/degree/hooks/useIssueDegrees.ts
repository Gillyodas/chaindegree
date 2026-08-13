import { useMutation, useQueryClient } from '@tanstack/react-query';
import { degreeApi } from '../degree.api';
import { degreeKeys } from '../degree.keys';
import type { IssueDegreeRequest, IssueDegreeResponse } from '../degree.types';

export interface IssueDegreesMutationParams {
  data: IssueDegreeRequest;
  idempotencyKey: string;
}

export function useIssueDegreesMutation() {
  const queryClient = useQueryClient();

  return useMutation<IssueDegreeResponse, Error, IssueDegreesMutationParams>({
    mutationFn: ({ data, idempotencyKey }) =>
      degreeApi.issueDegrees(data, idempotencyKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: degreeKeys.lists() });
    },
  });
}
