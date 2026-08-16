import { useState, useRef, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { verificationApi } from '../verification.api';
import { mapVerificationResponse, mapVerificationError } from '../verification.mapper';
import type {
  VerifyDegreeRequest,
  VerificationResultType,
} from '../verification.types';

export function useVerifyDegree() {
  const [result, setResult] = useState<VerificationResultType | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  const clearResult = useCallback(() => {
    setResult(null);
  }, []);

  const mutation = useMutation({
    mutationFn: async (request: VerifyDegreeRequest) => {
      // Abort any ongoing request before starting a new one
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      const controller = new AbortController();
      abortControllerRef.current = controller;

      const data = await verificationApi.verifyDegree(request, controller.signal);
      return data;
    },
    onSuccess: (data) => {
      const mapped = mapVerificationResponse(data);
      setResult(mapped);
    },
    onError: (error) => {
      // If error is due to cancellation/abort, ignore silently
      if (error instanceof Error && error.name === 'CanceledError') {
        return;
      }
      const mapped = mapVerificationError(error);
      setResult(mapped);
    },
  });

  const verify = useCallback(
    (request: VerifyDegreeRequest) => {
      mutation.mutate(request);
    },
    [mutation],
  );

  return {
    verify,
    result,
    isPending: mutation.isPending,
    clearResult,
    inputError: result?.kind === 'input_error' ? result.message : null,
  };
}
