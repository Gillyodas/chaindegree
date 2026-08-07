import { useNavigate } from 'react-router';
import { useAuth } from '@/app/providers/AuthProvider';
import type { UserRole } from '@/shared/types/api.types';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/shared/components/ui/card';
import { Button } from '@/shared/components/ui/button';
import { GraduationCap, ShieldCheck, Briefcase, UserCheck } from 'lucide-react';

const roleCards: {
  role: UserRole;
  title: string;
  name: string;
  email: string;
  description: string;
  icon: typeof GraduationCap;
}[] = [
  {
    role: 'Registrar',
    title: 'Education Registrar',
    name: 'Dr. Sarah Mitchell',
    email: 'registrar@chaindegree.edu',
    description: 'Issue degrees, manage degree updates, revoke degrees, and monitor batch processing.',
    icon: GraduationCap,
  },
  {
    role: 'Student',
    title: 'Student / Graduate',
    name: 'Alex Johnson',
    email: 'student@chaindegree.edu',
    description: 'View issued degrees, apply for jobs, and submit data integrity reports.',
    icon: UserCheck,
  },
  {
    role: 'Recruiter',
    title: 'Corporate Recruiter',
    name: 'Emily Davis',
    email: 'recruiter@techcorp.com',
    description: 'Post job offers with degree criteria, review applications, and verify candidate degrees.',
    icon: Briefcase,
  },
  {
    role: 'Admin',
    title: 'System Administrator',
    name: 'James Wilson',
    email: 'admin@chaindegree.io',
    description: 'Review complaints/reports, approve evidence, and manage institution reputation.',
    icon: ShieldCheck,
  },
];

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSelectRole = (role: UserRole) => {
    login(role);
    navigate('/');
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/40 p-4">
      <div className="w-full max-w-4xl space-y-6">
        <div className="text-center space-y-2">
          <div className="inline-flex items-center gap-2 text-primary font-bold text-2xl">
            <GraduationCap className="h-8 w-8" />
            <span>ChainDegree</span>
          </div>
          <h1 className="text-3xl font-bold tracking-tight">Select Demo Account Role</h1>
          <p className="text-muted-foreground max-w-lg mx-auto">
            Choose an actor role below to log in with pre-configured mock credentials and explore system features.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {roleCards.map((card) => {
            const Icon = card.icon;
            return (
              <Card
                key={card.role}
                className="cursor-pointer transition-all hover:border-primary hover:shadow-md group"
                onClick={() => handleSelectRole(card.role)}
              >
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-lg font-semibold flex items-center gap-2">
                    <Icon className="h-5 w-5 text-primary group-hover:scale-110 transition-transform" />
                    {card.title}
                  </CardTitle>
                  <span className="text-xs px-2.5 py-0.5 rounded-full bg-primary/10 text-primary font-medium">
                    {card.role}
                  </span>
                </CardHeader>
                <CardContent className="space-y-3 pt-2">
                  <div className="text-sm">
                    <p className="font-medium text-foreground">{card.name}</p>
                    <p className="text-muted-foreground text-xs">{card.email}</p>
                  </div>
                  <CardDescription className="text-xs">{card.description}</CardDescription>
                  <Button size="sm" className="w-full mt-2">
                    Log in as {card.role}
                  </Button>
                </CardContent>
              </Card>
            );
          })}
        </div>

        <div className="text-center text-xs text-muted-foreground">
          Public degree verification is available without logging in at{' '}
          <a href="/verify" className="text-primary underline">
            /verify
          </a>
        </div>
      </div>
    </div>
  );
}

export default LoginPage;
