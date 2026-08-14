import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatusBadge } from '../StatusBadge';
import type { DegreeStatus } from '@/shared/types/api.types';

describe('StatusBadge', () => {
  const statuses: { status: DegreeStatus; expectedLabel: string }[] = [
    { status: 'Pending_Confirmation', expectedLabel: 'Pending Confirmation' },
    { status: 'Confirmed', expectedLabel: 'Confirmed' },
    { status: 'Confirmation_Error', expectedLabel: 'Confirmation Error' },
    { status: 'Pending_Update', expectedLabel: 'Pending Update' },
    { status: 'Pending_Revocation', expectedLabel: 'Pending Revocation' },
    { status: 'Revoked', expectedLabel: 'Revoked' },
    { status: 'Frozen', expectedLabel: 'Frozen' },
  ];

  statuses.forEach(({ status, expectedLabel }) => {
    it(`should render correct label "${expectedLabel}" for status "${status}"`, () => {
      render(<StatusBadge status={status} />);
      expect(screen.getByText(expectedLabel)).toBeInTheDocument();
    });
  });

  it('should render fallback label for unknown status string without crashing', () => {
    render(<StatusBadge status="UNEXPECTED_NEW_STATUS" />);
    expect(screen.getByText('UNEXPECTED_NEW_STATUS')).toBeInTheDocument();
  });

  it('should render Unknown for empty status', () => {
    render(<StatusBadge status="" />);
    expect(screen.getByText('Unknown')).toBeInTheDocument();
  });
});
