import { describe, it, expect } from 'vitest';
import { verificationSchema } from '../verification.schema';
import { DEGREE_CODE_PATTERN } from '../verification.types';

describe('verification.schema', () => {
  it('validates a valid degreeCode without version', () => {
    const data = { degreeCode: 'DEG-2026-000001' };
    const result = verificationSchema.safeParse(data);
    expect(result.success).toBe(true);
  });

  it('validates a valid degreeCode with positive integer version', () => {
    const data = { degreeCode: 'DEG-2026-000001', version: 2 };
    const result = verificationSchema.safeParse(data);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.version).toBe(2);
    }
  });

  it('fails when degreeCode is empty', () => {
    const data = { degreeCode: '' };
    const result = verificationSchema.safeParse(data);
    expect(result.success).toBe(false);
  });

  it('fails when degreeCode is only whitespace', () => {
    const data = { degreeCode: '   ' };
    const result = verificationSchema.safeParse(data);
    expect(result.success).toBe(false);
  });

  it('fails when version is 0 (must be positive)', () => {
    const data = { degreeCode: 'DEG-2026-000001', version: 0 };
    const result = verificationSchema.safeParse(data);
    expect(result.success).toBe(false);
  });

  it('fails when version is negative', () => {
    const data = { degreeCode: 'DEG-2026-000001', version: -1 };
    const result = verificationSchema.safeParse(data);
    expect(result.success).toBe(false);
  });

  it('fails when version is float/decimal', () => {
    const data = { degreeCode: 'DEG-2026-000001', version: 1.5 };
    const result = verificationSchema.safeParse(data);
    expect(result.success).toBe(false);
  });

  describe('DEGREE_CODE_PATTERN', () => {
    it('matches DEG-YYYY-NNNNNN correctly', () => {
      expect(DEGREE_CODE_PATTERN.test('DEG-2026-000001')).toBe(true);
      expect(DEGREE_CODE_PATTERN.test('DEG-2025-999999')).toBe(true);
    });

    it('rejects invalid patterns', () => {
      expect(DEGREE_CODE_PATTERN.test('DEG-26-001')).toBe(false);
      expect(DEGREE_CODE_PATTERN.test('DEG-2026-1')).toBe(false);
      expect(DEGREE_CODE_PATTERN.test('deg-2026-000001')).toBe(false);
      expect(DEGREE_CODE_PATTERN.test('INVALID-CODE')).toBe(false);
      expect(DEGREE_CODE_PATTERN.test('')).toBe(false);
    });
  });
});
