import { createContext, useContext, useState, type ReactNode } from 'react';
import type { UserRole } from '@/shared/types/api.types';
import type { AuthContextType, MockUser } from '@/features/auth/types/auth.types';

const mockUsers: Record<UserRole, MockUser> = {
  Registrar: {
    id: 'mock-registrar-001',
    fullName: 'Dr. Sarah Mitchell',
    email: 'registrar@chaindegree.edu',
    role: 'Registrar',
    institutionId: 'inst-001',
    institutionName: 'ChainDegree University',
  },
  Student: {
    id: 'mock-student-001',
    fullName: 'Alex Johnson',
    email: 'student@chaindegree.edu',
    role: 'Student',
  },
  Recruiter: {
    id: 'mock-recruiter-001',
    fullName: 'Emily Davis',
    email: 'recruiter@techcorp.com',
    role: 'Recruiter',
  },
  Admin: {
    id: 'mock-admin-001',
    fullName: 'James Wilson',
    email: 'admin@chaindegree.io',
    role: 'Admin',
  },
};

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Default to Registrar for dev convenience
  const [currentUser, setCurrentUser] = useState<MockUser | null>(mockUsers.Registrar);

  const login = (role: UserRole) => {
    setCurrentUser(mockUsers[role]);
  };

  const logout = () => {
    setCurrentUser(null);
  };

  const switchRole = (role: UserRole) => {
    setCurrentUser(mockUsers[role]);
  };

  return (
    <AuthContext.Provider
      value={{
        currentUser,
        isAuthenticated: currentUser !== null,
        login,
        logout,
        switchRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
