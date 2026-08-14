import { describe, it, expect, beforeEach, vi } from 'vitest';
import { degreeApi } from '../degree.api';
import { httpClient } from '@/shared/api/http';

vi.mock('@/shared/api/http', async () => {
  const actual = await vi.importActual('@/shared/api/http');
  return {
    ...actual,
    httpClient: {
      post: vi.fn(),
      put: vi.fn(),
      get: vi.fn(),
    },
  };
});

describe('degreeApi Update & Revoke', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('updateDegree', () => {
    it('should send PUT request with correct URL, payload, and Idempotency-Key header', async () => {
      const mockResponse = {
        data: {
          degreeId: 'deg-1',
          currentStatus: 'Pending_Update',
          isShortcut: false,
          message: 'Degree update request accepted',
        },
      };
      (httpClient.put as ReturnType<typeof vi.fn>).mockResolvedValue(mockResponse);

      const requestData = {
        major: 'Data Science',
        classification: 'Excellent',
        reasonCode: 'CORRECTION',
      };
      const key = 'idem-key-update-1';

      const result = await degreeApi.updateDegree('deg-1', requestData, key);

      expect(httpClient.put).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees/deg-1',
        requestData,
        {
          headers: {
            'Idempotency-Key': key,
          },
        },
      );
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('revokeDegree', () => {
    it('should send POST request with correct revoke URL, payload, and Idempotency-Key header', async () => {
      const mockResponse = {
        data: {
          degreeId: 'deg-2',
          currentStatus: 'Revoked',
          isShortcut: true,
          message: 'Degree revoked successfully',
        },
      };
      (httpClient.post as ReturnType<typeof vi.fn>).mockResolvedValue(mockResponse);

      const requestData = {
        reasonCode: 'ACADEMIC_FRAUD',
      };
      const key = 'idem-key-revoke-1';

      const result = await degreeApi.revokeDegree('deg-2', requestData, key);

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees/deg-2/revoke',
        requestData,
        {
          headers: {
            'Idempotency-Key': key,
          },
        },
      );
      expect(result).toEqual(mockResponse.data);
    });
  });
});
