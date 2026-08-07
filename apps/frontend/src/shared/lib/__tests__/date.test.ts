import { describe, it, expect } from 'vitest';
import { formatDate, formatDateTime, formatRelativeTime } from '../date';

describe('date utils', () => {
  describe('formatDate', () => {
    it('should format ISO string to English date format (e.g. Aug 7, 2026)', () => {
      const result = formatDate('2026-08-07T12:00:00Z');
      expect(result).toMatch(/Aug 7, 2026/);
    });

    it('should return empty string for invalid date', () => {
      expect(formatDate('invalid-date')).toBe('');
    });
  });

  describe('formatDateTime', () => {
    it('should format ISO string to English date time format', () => {
      const result = formatDateTime('2026-08-07T12:30:00Z');
      expect(result).toMatch(/Aug 7, 2026/);
      expect(result).toMatch(/30/); // minutes part
    });

    it('should return empty string for invalid date', () => {
      expect(formatDateTime('invalid-date')).toBe('');
    });
  });

  describe('formatRelativeTime', () => {
    it('should format relative time in English', () => {
      const now = new Date();
      const pastMin = new Date(now.getTime() - 5 * 60 * 1000); // 5 mins ago
      const result = formatRelativeTime(pastMin);
      expect(result).toMatch(/5 minutes ago/);
    });

    it('should return empty string for invalid date', () => {
      expect(formatRelativeTime('invalid-date')).toBe('');
    });
  });
});
