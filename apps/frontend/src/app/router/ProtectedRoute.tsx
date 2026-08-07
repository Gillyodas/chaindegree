import { Navigate, Outlet } from 'react-router';
import { useAuth } from '@/app/providers/AuthProvider';
import type { UserRole } from '@/shared/types/api.types';

interface ProtectedRouteProps {
  allowedRoles?: UserRole[];
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { currentUser, isAuthenticated } = useAuth();

  if (!isAuthenticated || !currentUser) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && allowedRoles.length > 0 && !allowedRoles.includes(currentUser.role)) {
    return (
      <div className="flex h-full min-h-[400px] flex-col items-center justify-center p-8 text-center">
        <div className="rounded-full bg-destructive/10 p-3 text-destructive mb-3">
          <svg
            className="h-8 w-8"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <h2 className="text-xl font-bold">Access Forbidden</h2>
        <p className="mt-2 text-sm text-muted-foreground max-w-md">
          You do not have permission to access this page. Required role: {allowedRoles.join(', ')}. Current role: {currentUser.role}.
        </p>
      </div>
    );
  }

  return <Outlet />;
}

export default ProtectedRoute;
