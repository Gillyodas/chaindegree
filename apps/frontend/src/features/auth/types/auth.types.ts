import type { UserRole } from '@/shared/types/api.types';

export type MockUser = {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  institutionId?: string;
  institutionName?: string;
};

export type AuthContextType = {
  currentUser: MockUser | null;
  isAuthenticated: boolean;
  login: (role: UserRole) => void;
  logout: () => void;
  switchRole: (role: UserRole) => void;
};
