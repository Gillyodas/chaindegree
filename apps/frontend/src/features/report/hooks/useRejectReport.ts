import { useMutation, useQueryClient } from '@tanstack/react-query';
import { reportApi } from '../report.api';
import { reportKeys } from '../report.keys';
import type { RejectReportResponse } from '../report.types';
import { notification } from '@/shared/services/notification.service';
import { getErrorMessage } from '@/shared/api/error-mapper';

export interface RejectReportParams {
  id: string;
  reason: string;
}

export function useRejectReportMutation() {
  const queryClient = useQueryClient();

  return useMutation<RejectReportResponse, Error, RejectReportParams>({
    mutationFn: ({ id, reason }: RejectReportParams) =>
      reportApi.rejectReport(id, reason),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: reportKeys.all });
      notification.success('Report rejected successfully.');
    },
    onError: (error) => {
      const message = getErrorMessage(error);
      notification.error(message);
    },
  });
}
