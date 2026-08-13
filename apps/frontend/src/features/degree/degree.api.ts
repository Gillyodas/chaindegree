import { httpClient } from '@/shared/api/http';
import type {
  IssueDegreeRequest,
  IssueDegreeResponse,
  BatchStatusResponse,
  DegreeListItem,
  DegreeDetail,
} from './degree.types';

export const degreeApi = {
  issueDegrees: async (
    data: IssueDegreeRequest,
    idempotencyKey: string,
  ): Promise<IssueDegreeResponse> => {
    const response = await httpClient.post<IssueDegreeResponse>(
      '/api/v1/institutions/degrees',
      data,
      {
        headers: {
          'Idempotency-Key': idempotencyKey,
        },
      },
    );
    return response.data;
  },

  getBatchStatus: async (batchId: string): Promise<BatchStatusResponse> => {
    const response = await httpClient.get<BatchStatusResponse>(
      `/api/v1/institutions/degrees/batches/${batchId}`,
    );
    return response.data;
  },

  retryDegreeConfirmation: async (id: string): Promise<void> => {
    await httpClient.post(`/api/v1/institutions/degrees/${id}/retry`);
  },

  getDegrees: async (): Promise<DegreeListItem[]> => {
    const response = await httpClient.get<DegreeListItem[]>(
      '/api/v1/institutions/degrees',
    );
    return response.data;
  },

  getDegree: async (id: string): Promise<DegreeDetail> => {
    const response = await httpClient.get<DegreeDetail>(
      `/api/v1/institutions/degrees/${id}`,
    );
    return response.data;
  },
};
