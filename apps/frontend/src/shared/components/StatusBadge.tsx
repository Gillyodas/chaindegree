import type { DegreeStatus } from '@/shared/types/api.types';
import { Badge } from '@/shared/components/ui/badge';
import { cn } from '@/shared/lib/utils';

type StatusConfig = {
  label: string;
  className: string;
};

const statusConfigs: Record<DegreeStatus, StatusConfig> = {
  Pending_Confirmation: {
    label: 'Pending Confirmation',
    className: 'bg-amber-100 text-amber-800 border-amber-300 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  Confirmed: {
    label: 'Confirmed',
    className: 'bg-emerald-100 text-emerald-800 border-emerald-300 dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800',
  },
  Confirmation_Error: {
    label: 'Confirmation Error',
    className: 'bg-rose-100 text-rose-800 border-rose-300 dark:bg-rose-950 dark:text-rose-300 dark:border-rose-800',
  },
  Pending_Update: {
    label: 'Pending Update',
    className: 'bg-amber-100 text-amber-800 border-amber-300 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  Pending_Revocation: {
    label: 'Pending Revocation',
    className: 'bg-amber-100 text-amber-800 border-amber-300 dark:bg-amber-950 dark:text-amber-300 dark:border-amber-800',
  },
  Revoked: {
    label: 'Revoked',
    className: 'bg-red-100 text-red-800 border-red-300 dark:bg-red-950 dark:text-red-300 dark:border-red-800',
  },
  Frozen: {
    label: 'Frozen',
    className: 'bg-slate-100 text-slate-800 border-slate-300 dark:bg-slate-900 dark:text-slate-300 dark:border-slate-700',
  },
};

export interface StatusBadgeProps {
  status: DegreeStatus;
  className?: string;
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = statusConfigs[status] ?? {
    label: status,
    className: 'bg-muted text-muted-foreground',
  };

  return (
    <Badge
      variant="outline"
      className={cn('font-medium border shadow-none px-2.5 py-0.5 text-xs', config.className, className)}
    >
      {config.label}
    </Badge>
  );
}

export default StatusBadge;
