import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { HttpError } from '@/shared/api/http';
import { verificationApi } from '../verification.api';
import { verificationKeys } from '../verification.keys';
import { DEGREE_CODE_PATTERN, type DegreeVersionItem } from '../verification.types';

export function useDebouncedValue<T>(value: T, delayMs: number = 500): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delayMs);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delayMs]);

  return debouncedValue;
}

export function useDegreeVersions(degreeCode: string) {
  const trimmed = degreeCode?.trim() ?? '';
  const debouncedCode = useDebouncedValue(trimmed, 500);
  const isValidPattern = DEGREE_CODE_PATTERN.test(debouncedCode);

  const query = useQuery({
    queryKey: verificationKeys.versions(debouncedCode),
    queryFn: ({ signal }) => verificationApi.getDegreeVersions(debouncedCode, signal),
    enabled: isValidPattern,
    staleTime: 5000,
    gcTime: 60000,
    retry: false,
  });

  const isNotFound =
    query.isError &&
    query.error instanceof HttpError &&
    (query.error.status === 404 || query.error.errorCode === 'DEGREE_NOT_FOUND');

  return {
    versions: query.data?.versions ?? ([] as DegreeVersionItem[]),
    currentVersion: query.data?.currentVersion ?? null,
    isLoading: query.isLoading && query.fetchStatus !== 'idle',
    isFetching: query.isFetching,
    degreeNotFound: isNotFound,
    error: query.error,
    isValidPattern,
    isSuccess: query.isSuccess,
  };
}
