import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router';
import { ProtectedRoute } from '../ProtectedRoute';
import * as AuthModule from '@/app/providers/AuthProvider';

vi.mock('@/app/providers/AuthProvider', () => ({
  useAuth: vi.fn(),
}));

describe('ProtectedRoute', () => {
  it('should redirect to /login when user is not authenticated', () => {
    vi.mocked(AuthModule.useAuth).mockReturnValue({
      currentUser: null,
      isAuthenticated: false,
      login: vi.fn(),
      logout: vi.fn(),
      switchRole: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route path="/login" element={<div>Login Page</div>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/protected" element={<div>Protected Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Login Page')).toBeInTheDocument();
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
  });

  it('should render Access Forbidden when user role does not match allowedRoles', () => {
    vi.mocked(AuthModule.useAuth).mockReturnValue({
      currentUser: {
        id: '1',
        fullName: 'Student User',
        email: 'student@test.com',
        role: 'Student',
      },
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
      switchRole: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/admin-only']}>
        <Routes>
          <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
            <Route path="/admin-only" element={<div>Admin Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Access Forbidden')).toBeInTheDocument();
    expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
  });

  it('should render content when user is authenticated and role is allowed', () => {
    vi.mocked(AuthModule.useAuth).mockReturnValue({
      currentUser: {
        id: '1',
        fullName: 'Registrar User',
        email: 'registrar@test.com',
        role: 'Registrar',
      },
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
      switchRole: vi.fn(),
    });

    render(
      <MemoryRouter initialEntries={['/registrar-page']}>
        <Routes>
          <Route element={<ProtectedRoute allowedRoles={['Registrar']} />}>
            <Route path="/registrar-page" element={<div>Registrar Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('Registrar Content')).toBeInTheDocument();
  });
});
