import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { VerificationForm } from '../components/VerificationForm';
import type { DegreeVersionItem } from '../verification.types';

describe('VerificationForm', () => {
  const defaultProps = {
    onSubmit: vi.fn(),
    isSubmitting: false,
    inputError: null,
    onDegreeCodeChange: vi.fn(),
    onVersionChange: vi.fn(),
    degreeCode: '',
    onDegreeCodeInputChange: vi.fn(),
    versions: [] as DegreeVersionItem[],
    versionsLoading: false,
    degreeNotFound: false,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders input fields and submit button', () => {
    render(<VerificationForm {...defaultProps} />);

    expect(screen.getByLabelText(/degree code/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /verify degree/i })).toBeInTheDocument();
  });

  it('submits form with valid degree code', async () => {
    const onSubmit = vi.fn();
    render(
      <VerificationForm
        {...defaultProps}
        degreeCode="DEG-2026-000001"
        onSubmit={onSubmit}
      />,
    );

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        degreeCode: 'DEG-2026-000001',
        version: null,
      });
    });
  });

  it('calls onDegreeCodeInputChange and onDegreeCodeChange when input changes', async () => {
    const user = userEvent.setup();
    const onInputChange = vi.fn();
    const onCodeChange = vi.fn();

    render(
      <VerificationForm
        {...defaultProps}
        onDegreeCodeInputChange={onInputChange}
        onDegreeCodeChange={onCodeChange}
      />,
    );

    const input = screen.getByLabelText(/degree code/i);
    await user.type(input, 'DEG');

    expect(onInputChange).toHaveBeenCalled();
    expect(onCodeChange).toHaveBeenCalled();
  });

  it('shows fail-fast inline warning and disables button when degreeNotFound is true', () => {
    render(
      <VerificationForm
        {...defaultProps}
        degreeCode="DEG-2026-999999"
        degreeNotFound={true}
      />,
    );

    expect(
      screen.getByText(/No degree found with this code. Please check and try again./i),
    ).toBeInTheDocument();

    const submitBtn = screen.getByRole('button', { name: /verify degree/i });
    expect(submitBtn).toBeDisabled();
  });

  it('shows inline inputError when passed from parent', () => {
    render(
      <VerificationForm
        {...defaultProps}
        degreeCode="DEG-2026-000001"
        inputError="Salt must be a 16-character hexadecimal string."
      />,
    );

    expect(
      screen.getByText('Salt must be a 16-character hexadecimal string.'),
    ).toBeInTheDocument();
  });

  it('disables submit button and shows loading text when isSubmitting is true', () => {
    render(
      <VerificationForm
        {...defaultProps}
        degreeCode="DEG-2026-000001"
        isSubmitting={true}
      />,
    );

    const submitBtn = screen.getByRole('button', { name: /verifying degree\.\.\./i });
    expect(submitBtn).toBeDisabled();
  });
});
