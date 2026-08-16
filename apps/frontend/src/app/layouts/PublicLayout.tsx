import { Link, Outlet } from 'react-router';
import { Button } from '@/shared/components/ui/button';
import { GraduationCap } from 'lucide-react';

export function PublicLayout() {
  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Header */}
      <header className="flex h-16 items-center justify-between border-b px-6">
        <div className="flex items-center gap-6">
          <Link to="/" className="flex items-center gap-2 font-bold text-lg text-primary">
            <GraduationCap className="h-6 w-6" />
            <span>ChainDegree</span>
          </Link>
          <nav className="hidden sm:flex items-center gap-4 text-sm font-medium text-muted-foreground">
            <Link to="/verify" className="hover:text-foreground transition-colors">
              Verify Degree
            </Link>
          </nav>
        </div>

        <div className="flex items-center gap-3">
          <Button asChild variant="outline" size="sm">
            <Link to="/login">Log In</Link>
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1">
        <Outlet />
      </main>

      {/* Footer */}
      <footer className="border-t py-4 text-center text-xs text-muted-foreground">
        © 2026 ChainDegree — Blockchain Digital Degree Verification System.
      </footer>
    </div>
  );
}

export default PublicLayout;
