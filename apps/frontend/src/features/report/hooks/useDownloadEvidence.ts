import { useState } from 'react';
import { reportApi } from '../report.api';
import { notification } from '@/shared/services/notification.service';
import { getErrorMessage } from '@/shared/api/error-mapper';

export function useDownloadEvidence() {
  const [isDownloading, setIsDownloading] = useState(false);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  const downloadEvidence = async (reportId: string, fileName?: string) => {
    setIsDownloading(true);
    setDownloadingId(reportId);
    try {
      await reportApi.downloadReportEvidence(reportId, fileName);
    } catch (err: unknown) {
      const message = getErrorMessage(err);
      notification.error(message || 'Failed to download evidence file.');
    } finally {
      setIsDownloading(false);
      setDownloadingId(null);
    }
  };

  return {
    downloadEvidence,
    isDownloading,
    downloadingId,
  };
}
