import { useMutation, useQueryClient } from '@tanstack/react-query';
import { reportApi } from '../report.api';
import { reportKeys } from '../report.keys';
import type { ApproveReportResponse } from '../report.types';
import { notification } from '@/shared/services/notification.service';
import { getErrorMessage } from '@/shared/api/error-mapper';

export function useApproveReportMutation() {
  const queryClient = useQueryClient();

  return useMutation<ApproveReportResponse, Error, string>({
    mutationFn: (id: string) => reportApi.approveReport(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: reportKeys.all });
      notification.success(
        'Report approved successfully. Asynchronous revocation and reputation penalty processes have been initiated.',
      );
    },
    onError: (error) => {
      const message = getErrorMessage(error);
      notification.error(message);
    },
  });
}
