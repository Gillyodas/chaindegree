import { useState } from 'react';
import { Link } from 'react-router';
import { Plus, RefreshCw, Eye } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import { HttpError } from '@/shared/api/http';
import { useDegreesQuery, useRetryDegreeMutation } from '../hooks/useDegreeQueries';

export function DegreeListPage() {
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 10;

  const { data: degrees, isLoading, error, refetch } = useDegreesQuery();
  const retryMutation = useRetryDegreeMutation();

  const handleRetry = (id: string) => {
    retryMutation.mutate(id, {
      onSuccess: () => {
        toast.success('Retry request submitted successfully. Monitoring status update...');
      },
      onError: (err) => {
        toast.error(err.message || 'Failed to retry degree confirmation.');
      },
    });
  };

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  // Differentiate 404 / NotFound from Server / Network Error
  const isNotFound = error instanceof HttpError && error.type === 'not_found';

  if (error && !isNotFound) {
    return (
      <ErrorState
        title="Failed to load degrees"
        description={error.message || 'An error occurred while fetching the degree list.'}
        onRetry={() => refetch()}
      />
    );
  }

  const degreeList = degrees ?? [];
  const isEmpty = degreeList.length === 0 || isNotFound;

  // Pagination logic (client-side)
  const totalPages = Math.ceil(degreeList.length / pageSize) || 1;
  const paginatedDegrees = degreeList.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize,
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Degrees Management</h1>
          <p className="text-sm text-muted-foreground">
            View, track real-time status, and manage academic degree issuances.
          </p>
        </div>
        <Link to="/degrees/issue">
          <Button className="gap-2">
            <Plus className="h-4 w-4" />
            Issue Degrees
          </Button>
        </Link>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
          <div>
            <CardTitle className="text-lg font-semibold">Issued Degrees</CardTitle>
            <CardDescription>
              Total {degreeList.length} degree(s) recorded in system.
            </CardDescription>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            className="gap-1.5"
          >
            <RefreshCw className="h-3.5 w-3.5" />
            Refresh
          </Button>
        </CardHeader>

        <CardContent>
          {isEmpty ? (
            <EmptyState
              title="No degrees found"
              description="Issue your first degree to get started."
              action={
                <Link to="/degrees/issue">
                  <Button size="sm">
                    <Plus className="h-4 w-4 mr-1" />
                    Issue Degree Now
                  </Button>
                </Link>
              }
            />
          ) : (
            <div className="space-y-4">
              <div className="rounded-md border overflow-x-auto">
                <table className="w-full text-sm text-left">
                  <thead className="bg-muted/50 text-muted-foreground font-medium border-b">
                    <tr>
                      <th className="p-3">Degree Code</th>
                      <th className="p-3">Student Name</th>
                      <th className="p-3">Major</th>
                      <th className="p-3">Classification</th>
                      <th className="p-3">Status</th>
                      <th className="p-3">Issued Date</th>
                      <th className="p-3 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {paginatedDegrees.map((degree) => (
                      <tr
                        key={degree.id}
                        className="hover:bg-muted/30 transition-colors"
                      >
                        <td className="p-3 font-mono font-medium text-foreground">
                          <Link
                            to={`/degrees/${degree.id}`}
                            className="hover:underline text-primary"
                          >
                            {degree.degreeCode || degree.id.slice(0, 8)}
                          </Link>
                        </td>
                        <td className="p-3">
                          {degree.studentName || degree.studentId}
                        </td>
                        <td className="p-3">{degree.major}</td>
                        <td className="p-3">{degree.classification}</td>
                        <td className="p-3">
                          <StatusBadge status={degree.status} />
                        </td>
                        <td className="p-3 text-muted-foreground">
                          {new Date(degree.issuedAt).toLocaleDateString()}
                        </td>
                        <td className="p-3 text-right space-x-2">
                          {degree.status === 'Confirmation_Error' && (
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={retryMutation.isPending}
                              onClick={() => handleRetry(degree.id)}
                              className="text-xs text-rose-600 hover:text-rose-700 hover:bg-rose-50 border-rose-200"
                            >
                              <RefreshCw className="h-3 w-3 mr-1" />
                              Retry
                            </Button>
                          )}
                          <Link to={`/degrees/${degree.id}`}>
                            <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                              <Eye className="h-4 w-4" />
                              <span className="sr-only">View Details</span>
                            </Button>
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {totalPages > 1 && (
                <div className="flex items-center justify-between pt-2">
                  <p className="text-xs text-muted-foreground">
                    Page {currentPage} of {totalPages}
                  </p>
                  <div className="flex items-center space-x-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={currentPage === 1}
                      onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))}
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={currentPage === totalPages}
                      onClick={() => setCurrentPage((p) => Math.min(p + 1, totalPages))}
                    >
                      Next
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
