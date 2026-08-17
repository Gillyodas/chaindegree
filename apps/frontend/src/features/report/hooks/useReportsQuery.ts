import { useQuery } from '@tanstack/react-query';
import { reportApi } from '../report.api';
import { reportKeys } from '../report.keys';
import type { ReportListItem } from '../report.types';

export function useReportsQuery() {
  return useQuery<ReportListItem[], Error>({
    queryKey: reportKeys.lists(),
    queryFn: () => reportApi.getReports(),
  });
}
