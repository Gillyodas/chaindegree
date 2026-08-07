import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { AuthProvider, useAuth } from '../AuthProvider';

describe('AuthProvider (MockAuthProvider)', () => {
  it('should provide initial default mock user and isAuthenticated=true', () => {
    const { result } = renderHook(() => useAuth(), {
      wrapper: AuthProvider,
    });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.currentUser?.role).toBe('Registrar');
    expect(result.current.currentUser?.fullName).toBe('Dr. Sarah Mitchell');
  });

  it('should allow switching roles', () => {
    const { result } = renderHook(() => useAuth(), {
      wrapper: AuthProvider,
    });

    act(() => {
      result.current.switchRole('Admin');
    });

    expect(result.current.currentUser?.role).toBe('Admin');
    expect(result.current.currentUser?.fullName).toBe('James Wilson');

    act(() => {
      result.current.switchRole('Student');
    });

    expect(result.current.currentUser?.role).toBe('Student');
    expect(result.current.currentUser?.fullName).toBe('Alex Johnson');
  });

  it('should clear currentUser state on logout', () => {
    const { result } = renderHook(() => useAuth(), {
      wrapper: AuthProvider,
    });

    act(() => {
      result.current.logout();
    });

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.currentUser).toBeNull();
  });
});
