import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ErrorBoundary } from '../ErrorBoundary';

import type { ReactNode } from 'react';

function ProblematicComponent(): ReactNode {
  throw new Error('Test Component Render Crash');
}

describe('ErrorBoundary', () => {
  let consoleErrorSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    // Suppress console.error during intentional render crash test to keep output clean
    consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    consoleErrorSpy.mockRestore();
  });

  it('should render children normally when there is no error', () => {
    render(
      <ErrorBoundary>
        <div>Normal Content</div>
      </ErrorBoundary>,
    );

    expect(screen.getByText('Normal Content')).toBeInTheDocument();
  });

  it('should catch React render error and render fallback UI in English', () => {
    render(
      <ErrorBoundary>
        <ProblematicComponent />
      </ErrorBoundary>,
    );

    expect(screen.getByText('Something Went Wrong')).toBeInTheDocument();
    expect(
      screen.getByText('An unexpected error occurred in the application user interface.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Test Component Render Crash')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });
});
