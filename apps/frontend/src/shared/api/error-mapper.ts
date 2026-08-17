import { HttpError } from './http';

const businessErrorMessages: Record<string, string> = {
  DEGREE_ALREADY_EXISTS:
    'A degree with identical details has already been issued for this student.',
  CRYPTO_HASH_MISMATCH:
    'Verification failed. The provided data does not match official records.',
  BLOCKCHAIN_INVALID:
    'Blockchain verification failed. Data integrity cannot be confirmed.',
  DEGREE_NOT_FOUND: 'No degree found with the specified code.',
  UNSUPPORTED_VERSION: 'The specified degree version is not supported.',
  INVALID_SALT_FORMAT: 'Salt must be a 16-character hexadecimal string.',
  FILTER_CRITERIA_NOT_SATISFIED:
    'Your degree does not meet the minimum requirements for this position.',
  'Report.NotFound': 'The specified report was not found.',
  'Report.UnauthorizedReporter': 'Only Students and Recruiters are authorized to submit reports.',
  'Report.StudentCannotReportOthersDegree': 'Students are only permitted to submit reports for degrees issued to themselves.',
  'Report.EvidenceRequired': 'Evidence file is required when submitting a report.',
  'Report.AlreadyExistsUnderReview': 'A report for this degree is already under review by your account.',
  'Report.InvalidEvidenceFormat': 'The evidence file signature or content type is invalid. Only valid PDF, PNG, or JPG files are allowed.',
  'Report.UnauthorizedEvidenceDownload': 'You do not have permission to download evidence for this report.',
  'Report.EmptyRejectionReason': 'A valid reason must be provided when rejecting a report.',
  'Report.AlreadyReviewed': 'This report has already been reviewed.',
};

export function getBusinessErrorMessage(errorCode?: string): string | null {
  if (!errorCode) return null;
  return businessErrorMessages[errorCode] ?? null;
}

export function getErrorMessage(error: unknown): string {
  if (error instanceof HttpError) {
    if (error.errorCode) {
      const mappedMsg = getBusinessErrorMessage(error.errorCode);
      if (mappedMsg) return mappedMsg;
    }
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'An unexpected error occurred. Please try again.';
}
