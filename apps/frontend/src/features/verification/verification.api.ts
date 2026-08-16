import { httpClient } from '@/shared/api/http';
import type {
  VerifyDegreeRequest,
  VerifyDegreeSuccessResponse,
  DegreeVersionListResponse,
} from './verification.types';

export const verificationApi = {
  verifyDegree: async (
    request: VerifyDegreeRequest,
    signal?: AbortSignal,
  ): Promise<VerifyDegreeSuccessResponse> => {
    const response = await httpClient.post<VerifyDegreeSuccessResponse>(
      '/api/v1/institutions/degrees/verify',
      request,
      { signal },
    );
    return response.data;
  },

  getDegreeVersions: async (
    degreeCode: string,
    signal?: AbortSignal,
  ): Promise<DegreeVersionListResponse> => {
    const response = await httpClient.get<DegreeVersionListResponse>(
      `/api/v1/institutions/degrees/${encodeURIComponent(degreeCode)}/versions`,
      { signal },
    );
    return response.data;
  },
};
