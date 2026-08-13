import { describe, it, expect, beforeEach, vi } from 'vitest';
import { degreeApi } from '../degree.api';
import { httpClient, HttpError } from '@/shared/api/http';

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

describe('degreeApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('issueDegrees', () => {
    it('should send POST request with correct URL, payload, and Idempotency-Key header', async () => {
      const mockResponse = {
        data: {
          message: 'Success',
          acceptedCount: 1,
          degreeIds: ['deg-1'],
          failures: [],
        },
      };
      (httpClient.post as ReturnType<typeof vi.fn>).mockResolvedValue(mockResponse);

      const requestPayload = {
        degrees: [
          {
            studentId: '550e8400-e29b-41d4-a716-446655440000',
            major: 'Software Engineering',
            classification: 'Excellent',
            issuedAt: '2026-06-15T08:00:00Z',
          },
        ],
      };
      const key = 'idem-key-123';

      const result = await degreeApi.issueDegrees(requestPayload, key);

      expect(httpClient.post).toHaveBeenCalledWith(
        '/api/v1/institutions/degrees',
        requestPayload,
        {
          headers: {
            'Idempotency-Key': key,
          },
        },
      );
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('getBatchStatus', () => {
    it('should send GET request to correct batch URL', async () => {
      const mockData = { batchId: 'b-1', status: 'Processing' };
      (httpClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockData });

      const result = await degreeApi.getBatchStatus('b-1');

      expect(httpClient.get).toHaveBeenCalledWith('/api/v1/institutions/degrees/batches/b-1');
      expect(result).toEqual(mockData);
    });
  });

  describe('retryDegreeConfirmation', () => {
    it('should send POST request to correct retry URL', async () => {
      (httpClient.post as ReturnType<typeof vi.fn>).mockResolvedValue({});

      await degreeApi.retryDegreeConfirmation('deg-99');

      expect(httpClient.post).toHaveBeenCalledWith('/api/v1/institutions/degrees/deg-99/retry');
    });
  });

  describe('getDegrees', () => {
    it('should send GET request with pageIndex and pageSize params', async () => {
      const mockPagedResult = {
        items: [{ id: '1', degreeCode: 'DEG-1', status: 'Confirmed' }],
        totalCount: 1,
        pageIndex: 1,
        pageSize: 20,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      (httpClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockPagedResult });

      const result = await degreeApi.getDegrees(1, 20);

      expect(httpClient.get).toHaveBeenCalledWith('/api/v1/institutions/degrees', {
        params: { pageIndex: 1, pageSize: 20 },
      });
      expect(result).toEqual(mockPagedResult);
    });

    it('should throw error when GET request fails (no fallback in API layer)', async () => {
      const notFoundError = new HttpError('not_found', 404, 'Not found');
      (httpClient.get as ReturnType<typeof vi.fn>).mockRejectedValue(notFoundError);

      await expect(degreeApi.getDegrees()).rejects.toThrow(HttpError);
    });
  });

  describe('getDegree', () => {
    it('should send GET request to fetch single degree detail', async () => {
      const mockDetail = { id: 'deg-1', degreeCode: 'DEG-1' };
      (httpClient.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: mockDetail });

      const result = await degreeApi.getDegree('deg-1');

      expect(httpClient.get).toHaveBeenCalledWith('/api/v1/institutions/degrees/deg-1');
      expect(result).toEqual(mockDetail);
    });
  });
});
