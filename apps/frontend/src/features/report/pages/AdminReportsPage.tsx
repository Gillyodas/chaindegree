import { RefreshCw, ShieldAlert } from 'lucide-react';
import { Button } from '@/shared/components/ui/button';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { useReportsQuery } from '../hooks/useReportsQuery';
import { ReportListTable } from '../components/ReportListTable';

export function AdminReportsPage() {
  const { data: reports, isLoading, error, refetch, isRefetching } = useReportsQuery();

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Report Management</h1>
          <p className="text-muted-foreground text-sm">
            Review and process academic degree dispute and fraudulent data complaints.
          </p>
        </div>
        <ErrorState
          title="Failed to load reports"
          description={error.message || 'An unexpected error occurred while fetching reports.'}
          onRetry={() => refetch()}
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
            <ShieldAlert className="h-6 w-6 text-amber-500" />
            Report Management
          </h1>
          <p className="text-muted-foreground text-sm mt-1">
            Review and process academic degree dispute and fraudulent data complaints.
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={() => refetch()}
          disabled={isRefetching}
          className="gap-2"
        >
          <RefreshCw className={`h-4 w-4 ${isRefetching ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      {!reports || reports.length === 0 ? (
        <EmptyState
          title="No reports available"
          description="There are currently no submitted degree complaints or dispute reports pending review."
        />
      ) : (
        <ReportListTable reports={reports} onActionComplete={() => refetch()} />
      )}
    </div>
  );
}

export default AdminReportsPage;
