import { describe, it, expect } from 'vitest';
import {
  submitReportSchema,
  MAX_FILE_SIZE_BYTES,
  isValidFileExtension,
} from '../report.schema';

describe('report.schema', () => {
  const validDegreeId = '550e8400-e29b-41d4-a716-446655440000';

  const createMockFile = (name: string, size: number, type: string): File => {
    const buffer = new ArrayBuffer(size);
    return new File([buffer], name, { type });
  };

  it('validates a valid report submission payload', () => {
    const validFile = createMockFile('transcript.pdf', 1024 * 1024, 'application/pdf');
    const result = submitReportSchema.safeParse({
      degreeId: validDegreeId,
      reportType: 'Administrative_Error',
      description: 'The student classification is incorrectly listed as Good instead of Excellent.',
      evidenceFile: validFile,
    });

    expect(result.success).toBe(true);
  });

  it('rejects invalid degreeId UUID', () => {
    const validFile = createMockFile('transcript.pdf', 1024, 'application/pdf');
    const result = submitReportSchema.safeParse({
      degreeId: 'invalid-id',
      reportType: 'Administrative_Error',
      description: 'Valid description with more than ten characters.',
      evidenceFile: validFile,
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Invalid degree ID.');
    }
  });

  it('rejects invalid reportType', () => {
    const validFile = createMockFile('transcript.pdf', 1024, 'application/pdf');
    const result = submitReportSchema.safeParse({
      degreeId: validDegreeId,
      reportType: 'Unknown_Type',
      description: 'Valid description with more than ten characters.',
      evidenceFile: validFile,
    });

    expect(result.success).toBe(false);
  });

  it('rejects description shorter than 10 characters', () => {
    const validFile = createMockFile('transcript.pdf', 1024, 'application/pdf');
    const result = submitReportSchema.safeParse({
      degreeId: validDegreeId,
      reportType: 'Administrative_Error',
      description: 'Short',
      evidenceFile: validFile,
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('at least 10 characters');
    }
  });

  it('rejects description exceeding 2000 characters', () => {
    const validFile = createMockFile('transcript.pdf', 1024, 'application/pdf');
    const result = submitReportSchema.safeParse({
      degreeId: validDegreeId,
      reportType: 'Fraudulent_Data',
      description: 'A'.repeat(2001),
      evidenceFile: validFile,
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('cannot exceed 2000 characters');
    }
  });

  it('rejects empty file (0 bytes)', () => {
    const emptyFile = createMockFile('empty.pdf', 0, 'application/pdf');
    const result = submitReportSchema.safeParse({
      degreeId: validDegreeId,
      reportType: 'Administrative_Error',
      description: 'Valid description with more than ten characters.',
      evidenceFile: emptyFile,
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Evidence file cannot be empty.');
    }
  });

  it('rejects file larger than 5MB', () => {
    const largeFile = createMockFile('large.pdf', MAX_FILE_SIZE_BYTES + 1024, 'application/pdf');
    const result = submitReportSchema.safeParse({
      degreeId: validDegreeId,
      reportType: 'Administrative_Error',
      description: 'Valid description with more than ten characters.',
      evidenceFile: largeFile,
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('File size must not exceed 5MB.');
    }
  });

  it('rejects unsupported file extensions (e.g. .exe, .sh)', () => {
    const exeFile = createMockFile('virus.exe', 1024, 'application/octet-stream');
    const result = submitReportSchema.safeParse({
      degreeId: validDegreeId,
      reportType: 'Administrative_Error',
      description: 'Valid description with more than ten characters.',
      evidenceFile: exeFile,
    });

    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Only PDF, PNG, and JPG files are supported.');
    }
  });

  it('correctly detects valid and invalid file extensions via helper', () => {
    expect(isValidFileExtension('evidence.pdf')).toBe(true);
    expect(isValidFileExtension('screenshot.PNG')).toBe(true);
    expect(isValidFileExtension('photo.jpg')).toBe(true);
    expect(isValidFileExtension('photo.jpeg')).toBe(true);
    expect(isValidFileExtension('malware.exe')).toBe(false);
    expect(isValidFileExtension('script.sh')).toBe(false);
  });
});
