import { Link, NavLink, Outlet, useNavigate, useLocation } from 'react-router';
import { useAuth } from '@/app/providers/AuthProvider';
import type { UserRole } from '@/shared/types/api.types';
import { Button } from '@/shared/components/ui/button';
import { Badge } from '@/shared/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu';
import {
  GraduationCap,
  LayoutDashboard,
  FileCheck,
  PlusCircle,
  Briefcase,
  UserCheck,
  FileText,
  Award,
  LogOut,
  ChevronDown,
  Menu,
} from 'lucide-react';
import { useState } from 'react';

type NavItem = {
  label: string;
  to: string;
  icon: typeof LayoutDashboard;
  roles: UserRole[];
};

const navItems: NavItem[] = [
  {
    label: 'Dashboard',
    to: '/',
    icon: LayoutDashboard,
    roles: ['Registrar', 'Student', 'Recruiter', 'Admin'],
  },
  {
    label: 'Degrees',
    to: '/degrees',
    icon: FileCheck,
    roles: ['Registrar'],
  },
  {
    label: 'Issue Degree',
    to: '/degrees/issue',
    icon: PlusCircle,
    roles: ['Registrar'],
  },
  {
    label: 'My Degrees',
    to: '/degrees',
    icon: FileCheck,
    roles: ['Student'],
  },
  {
    label: 'Browse Jobs',
    to: '/jobs',
    icon: Briefcase,
    roles: ['Student', 'Recruiter'],
  },
  {
    label: 'My Applications',
    to: '/applications',
    icon: UserCheck,
    roles: ['Student'],
  },
  {
    label: 'Applicants',
    to: '/applications',
    icon: UserCheck,
    roles: ['Recruiter'],
  },
  {
    label: 'Reports Review',
    to: '/admin/reports',
    icon: FileText,
    roles: ['Admin'],
  },
  {
    label: 'Reputation',
    to: '/reputation',
    icon: Award,
    roles: ['Admin', 'Registrar', 'Student', 'Recruiter'],
  },
];

const allRoles: UserRole[] = ['Registrar', 'Student', 'Recruiter', 'Admin'];

export function DashboardLayout() {
  const { currentUser, logout, switchRole } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const filteredNavItems = navItems.filter(
    (item) => currentUser && item.roles.includes(currentUser.role),
  );

  return (
    <div className="flex min-h-screen bg-muted/20">
      {/* Sidebar */}
      <aside
        className={`fixed inset-y-0 left-0 z-40 w-64 border-r bg-background flex flex-col transition-transform duration-200 lg:static lg:translate-x-0 ${
          mobileMenuOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {/* Sidebar Header */}
        <div className="flex h-16 items-center gap-2 border-b px-6">
          <GraduationCap className="h-6 w-6 text-primary" />
          <span className="font-bold text-lg tracking-tight">ChainDegree</span>
        </div>

        {/* Navigation */}
        <nav className="flex-1 space-y-1 p-4 overflow-y-auto">
          {filteredNavItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to + item.label}
                to={item.to}
                end={item.to === '/'}
                onClick={() => setMobileMenuOpen(false)}
                className={({ isActive }) =>
                  `flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                    isActive
                      ? 'bg-primary text-primary-foreground'
                      : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
                  }`
                }
              >
                <Icon className="h-4 w-4" />
                {item.label}
              </NavLink>
            );
          })}

          <div className="pt-4 border-t my-2">
            <Link
              to="/verify"
              className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted-foreground hover:bg-accent hover:text-accent-foreground"
            >
              <FileCheck className="h-4 w-4" />
              Public Verification Portal
            </Link>
          </div>
        </nav>

        {/* User Info & Footer */}
        <div className="border-t p-4 space-y-3">
          <div className="flex items-center justify-between">
            <div className="truncate text-xs">
              <p className="font-semibold text-foreground truncate">{currentUser?.fullName}</p>
              <p className="text-muted-foreground truncate">{currentUser?.email}</p>
            </div>
            <Badge variant="outline" className="text-[10px] shrink-0">
              {currentUser?.role}
            </Badge>
          </div>

          <Button
            variant="ghost"
            size="sm"
            onClick={handleLogout}
            className="w-full justify-start text-destructive hover:text-destructive hover:bg-destructive/10"
          >
            <LogOut className="h-4 w-4 mr-2" />
            Log out
          </Button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Header */}
        <header className="flex h-16 items-center justify-between border-b bg-background px-6">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon"
              className="lg:hidden"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
            >
              <Menu className="h-5 w-5" />
            </Button>
            <h1 className="text-lg font-semibold capitalize">
              {location.pathname === '/'
                ? 'Dashboard'
                : location.pathname.substring(1).replace('-', ' ')}
            </h1>
          </div>

          {/* Quick Role Switcher Dropdown (Dev Helper) */}
          <div className="flex items-center gap-3">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="sm" className="gap-2 text-xs">
                  <span>Switch Role:</span>
                  <Badge variant="secondary" className="text-[10px]">
                    {currentUser?.role}
                  </Badge>
                  <ChevronDown className="h-3 w-3" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel className="text-xs">Quick Switch Role (Dev)</DropdownMenuLabel>
                <DropdownMenuSeparator />
                {allRoles.map((role) => (
                  <DropdownMenuItem
                    key={role}
                    onClick={() => switchRole(role)}
                    className={role === currentUser?.role ? 'font-bold text-primary' : ''}
                  >
                    {role}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>

        {/* Content Body */}
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default DashboardLayout;
