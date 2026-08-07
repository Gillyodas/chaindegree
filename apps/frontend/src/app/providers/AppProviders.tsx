import type { ReactNode } from 'react';
import { AuthProvider } from './AuthProvider';
import { QueryProvider } from './QueryProvider';
import { ThemeProvider } from './ThemeProvider';
import { Toaster } from '@/shared/components/ui/sonner';

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider defaultTheme="system">
      <AuthProvider>
        <QueryProvider>
          {children}
          <Toaster position="top-right" richColors />
        </QueryProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default AppProviders;
