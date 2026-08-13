import { describe, it, expect } from 'vitest';
import { issueDegreeItemSchema, issueDegreeFormSchema } from '../components/IssueDegreeForm';

describe('IssueDegreeForm Zod Schema', () => {
  it('should accept valid UUID studentId and required fields', () => {
    const validData = {
      degrees: [
        {
          studentId: '550e8400-e29b-41d4-a716-446655440000',
          major: 'Software Engineering',
          classification: 'Excellent',
          issuedAt: '2026-06-15',
        },
      ],
    };

    const result = issueDegreeFormSchema.safeParse(validData);
    expect(result.success).toBe(true);
  });

  it('should reject invalid UUID studentId format', () => {
    const invalidData = {
      degrees: [
        {
          studentId: 'not-a-valid-uuid',
          major: 'Software Engineering',
          classification: 'Excellent',
          issuedAt: '2026-06-15',
        },
      ],
    };

    const result = issueDegreeFormSchema.safeParse(invalidData);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toContain('Must be a valid UUID');
    }
  });

  it('should validate classification as required free-text without enum restriction', () => {
    const customClassificationData = {
      degrees: [
        {
          studentId: '550e8400-e29b-41d4-a716-446655440000',
          major: 'Computer Science',
          classification: 'Custom Distinction Honors',
          issuedAt: '2026-06-15',
        },
      ],
    };

    const result = issueDegreeFormSchema.safeParse(customClassificationData);
    expect(result.success).toBe(true);
  });

  it('should reject empty fields', () => {
    const emptyItem = {
      studentId: '550e8400-e29b-41d4-a716-446655440000',
      major: '',
      classification: '',
      issuedAt: '',
    };

    const result = issueDegreeItemSchema.safeParse(emptyItem);
    expect(result.success).toBe(false);
  });
});
