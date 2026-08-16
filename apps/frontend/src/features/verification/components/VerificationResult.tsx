import type { VerificationResultType } from '../verification.types';
import { VerifiedResult } from './VerifiedResult';
import { RevokedResult } from './RevokedResult';
import { TamperedWarning } from './TamperedWarning';
import { NotFoundResult } from './NotFoundResult';
import { VerificationError } from './VerificationError';

export interface VerificationResultProps {
  result: VerificationResultType | null;
  onRetry?: () => void;
}

export function VerificationResult({ result, onRetry }: VerificationResultProps) {
  if (!result) {
    return null;
  }

  switch (result.kind) {
    case 'valid':
      return <VerifiedResult data={result.data} />;

    case 'revoked':
      return <RevokedResult data={result.data} />;

    case 'tampered':
      return <TamperedWarning errorCode={result.errorCode} message={result.message} />;

    case 'not_found':
      return <NotFoundResult errorCode={result.errorCode} message={result.message} />;

    case 'input_error':
      // Input validation errors are rendered inline within the form
      return null;

    case 'rate_limited':
      return (
        <VerificationError
          message={result.message}
          isRateLimited
          onRetry={onRetry}
        />
      );

    case 'server_error':
      return <VerificationError message={result.message} onRetry={onRetry} />;

    default: {
      // Defensive fallback — never blank or crash
      return (
        <VerificationError
          message="An unrecognized verification outcome occurred. Please try again."
          onRetry={onRetry}
        />
      );
    }
  }
}
