import axios, {
  AxiosError,
  type AxiosInstance,
  type InternalAxiosRequestConfig,
  type AxiosResponse,
} from 'axios';
import { env } from '@/app/config/env';

export type HttpErrorType =
  | 'not_found'
  | 'forbidden'
  | 'conflict'
  | 'validation'
  | 'server_error'
  | 'timeout'
  | 'network'
  | 'unauthorized';

export class HttpError extends Error {
  public readonly type: HttpErrorType;
  public readonly status: number | null;
  public readonly errorCode?: string;
  public readonly details?: Record<string, string[]>;

  constructor(
    type: HttpErrorType,
    status: number | null,
    message: string,
    errorCode?: string,
    details?: Record<string, string[]>,
  ) {
    super(message);
    this.name = 'HttpError';
    this.type = type;
    this.status = status;
    this.errorCode = errorCode;
    this.details = details;
  }
}

export type TokenProvider = () => string | null;

let getAccessToken: TokenProvider = () => null;

export function configureHttpAuth(tokenProvider: TokenProvider) {
  getAccessToken = tokenProvider;
}

export const httpClient: AxiosInstance = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: env.apiTimeout,
  headers: {
    'Content-Type': 'application/json',
  },
});

httpClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

httpClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  (error: AxiosError<{ errorCode?: string; message?: string; details?: Record<string, string[]> }>) => {
    if (axios.isCancel(error)) {
      return Promise.reject(error);
    }

    if (!error.response) {
      if (error.code === 'ECONNABORTED' || error.message.includes('timeout')) {
        return Promise.reject(
          new HttpError(
            'timeout',
            null,
            'Request timed out. Please check your connection and try again.',
          ),
        );
      }
      return Promise.reject(
        new HttpError(
          'network',
          null,
          'Unable to connect to the server. Please check your internet connection.',
        ),
      );
    }

    const { status, data } = error.response;
    const errorCode = data?.errorCode;
    const serverMessage = data?.message;
    const details = data?.details;

    switch (status) {
      case 401:
        if (typeof window !== 'undefined') {
          window.location.href = '/login';
        }
        return Promise.reject(
          new HttpError('unauthorized', status, 'Session expired. Please log in again.', errorCode, details),
        );

      case 403:
        return Promise.reject(
          new HttpError(
            'forbidden',
            status,
            serverMessage ?? 'You do not have permission to perform this action.',
            errorCode,
            details,
          ),
        );

      case 404:
        return Promise.reject(
          new HttpError(
            'not_found',
            status,
            serverMessage ?? 'The requested resource was not found.',
            errorCode,
            details,
          ),
        );

      case 409:
        return Promise.reject(
          new HttpError(
            'conflict',
            status,
            serverMessage ?? 'A conflict occurred with existing data.',
            errorCode,
            details,
          ),
        );

      case 422:
        return Promise.reject(
          new HttpError(
            'validation',
            status,
            serverMessage ?? 'Validation failed for the submitted data.',
            errorCode,
            details,
          ),
        );

      case 500:
      default:
        return Promise.reject(
          new HttpError(
            'server_error',
            status,
            'Something went wrong on our end. Please try again later.',
            errorCode,
            details,
          ),
        );
    }
  },
);
