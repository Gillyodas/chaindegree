import { describe, it, expect, beforeEach, vi } from 'vitest';
import { verificationApi } from '../verification.api';
import { httpClient } from '@/shared/api/http';

vi.mock('@/shared/api/http', async () => {
  const actual = await vi.importActual('@/shared/api/http');
  return {
    ...actual,
    httpClient: {
      post: vi.fn(),
      get: vi.fn(),
    },
  };
});

describe('verificationApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('verifyDegree', () => {
    it('sends POST request to /api/v1/institutions/degrees/verify with request payload', async () => {
      const mockResponse = {
        data: {
          verified: true,
          status: 'Confirmed',
          degreeCode: 'DEG-2026-000001',
          version: 1,
        },
      };
      (httpClient.post as ReturnType<typeof vi.fn>).mockResolvedValue(mockResponse);

      const request = { degreeCode: 'DEG-2026-000001', version: 1 };
      const result = await verificationApi.verifyDegree(request);

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees/verify',
        request,
        { signal: undefined },
      );
      expect(result).toEqual(mockResponse.data);
    });

    it('passes abort signal when provided', async () => {
      const mockResponse = { data: { verified: true } };
      (httpClient.post as ReturnType<typeof vi.fn>).mockResolvedValue(mockResponse);

      const controller = new AbortController();
      const request = { degreeCode: 'DEG-2026-000001' };
      await verificationApi.verifyDegree(request, controller.signal);

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees/verify',
        request,
        { signal: controller.signal },
      );
    });
  });

  describe('getDegreeVersions', () => {
    it('sends GET request to /api/v1/institutions/degrees/{degreeCode}/versions', async () => {
      const mockResponse = {
        data: {
          degreeCode: 'DEG-2026-000001',
          currentVersion: 2,
          versions: [
            { version: 2, effectiveAt: '2026-07-01T00:00:00Z', isCurrent: true },
            { version: 1, effectiveAt: '2026-06-01T00:00:00Z', isCurrent: false },
          ],
        },
      };
      (httpClient.get as ReturnType<typeof vi.fn>).mockResolvedValue(mockResponse);

      const result = await verificationApi.getDegreeVersions('DEG-2026-000001');

      expect(httpClient.get).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees/DEG-2026-000001/versions',
        { signal: undefined },
      );
      expect(result).toEqual(mockResponse.data);
    });

    it('properly encodes degreeCode in URI', async () => {
      const mockResponse = { data: { versions: [] } };
      (httpClient.get as ReturnType<typeof vi.fn>).mockResolvedValue(mockResponse);

      await verificationApi.getDegreeVersions('DEG 2026/01');

      expect(httpClient.get).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees/DEG%202026%2F01/versions',
        { signal: undefined },
      );
    });
  });
});
