import { useMutation, useQueryClient } from '@tanstack/react-query';
import { reportApi } from '../report.api';
import { reportKeys } from '../report.keys';
import type { SubmitReportRequest, SubmitReportResponse } from '../report.types';
import { notification } from '@/shared/services/notification.service';
import { getErrorMessage } from '@/shared/api/error-mapper';

export function useSubmitReportMutation() {
  const queryClient = useQueryClient();

  return useMutation<SubmitReportResponse, Error, SubmitReportRequest>({
    mutationFn: async (request: SubmitReportRequest) => {
      const formData = new FormData();
      formData.append('degreeId', request.degreeId);
      formData.append('reportType', request.reportType);
      formData.append('description', request.description);
      formData.append('evidenceFile', request.evidenceFile);

      return reportApi.submitReport(formData);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: reportKeys.all });
      notification.success(
        'Report submitted successfully. The system will review it as soon as possible.',
      );
    },
    onError: (error) => {
      const message = getErrorMessage(error);
      notification.error(message);
    },
  });
}
