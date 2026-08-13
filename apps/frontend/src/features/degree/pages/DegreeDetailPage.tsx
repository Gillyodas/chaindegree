import { useParams, Link } from 'react-router';
import { ArrowLeft, Edit3, ShieldOff, AlertTriangle } from 'lucide-react';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import { HttpError } from '@/shared/api/http';
import { useDegreeDetailQuery } from '../hooks/useDegreeQueries';

export function DegreeDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: degree, isLoading, error, refetch } = useDegreeDetailQuery(id || '');

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  const isNotFound = error instanceof HttpError && error.type === 'not_found';

  if (isNotFound) {
    return (
      <div className="space-y-4">
        <Link to="/degrees">
          <Button variant="ghost" size="sm" className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back to Degrees
          </Button>
        </Link>
        <EmptyState
          title="Degree not found"
          description="The requested degree record does not exist or may have been removed."
        />
      </div>
    );
  }

  if (error || !degree) {
    return (
      <div className="space-y-4">
        <Link to="/degrees">
          <Button variant="ghost" size="sm" className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back to Degrees
          </Button>
        </Link>
        <ErrorState
          title="Failed to load degree details"
          description={error?.message || 'An unexpected error occurred.'}
          onRetry={() => refetch()}
        />
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      <div className="flex items-center justify-between">
        <Link to="/degrees">
          <Button variant="ghost" size="sm" className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back to Degrees
          </Button>
        </Link>

        {/* Action placeholders for future phases */}
        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            size="sm"
            disabled
            title="Available in Phase 2"
            className="opacity-60 cursor-not-allowed"
          >
            <Edit3 className="h-4 w-4 mr-1.5" />
            Update (Phase 2)
          </Button>

          <Button
            variant="outline"
            size="sm"
            disabled
            title="Available in Phase 2"
            className="opacity-60 cursor-not-allowed text-destructive border-destructive/30"
          >
            <ShieldOff className="h-4 w-4 mr-1.5" />
            Revoke (Phase 2)
          </Button>

          <Button
            variant="outline"
            size="sm"
            disabled
            title="Available in Phase 4"
            className="opacity-60 cursor-not-allowed text-amber-600 border-amber-200"
          >
            <AlertTriangle className="h-4 w-4 mr-1.5" />
            Report Issue (Phase 4)
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader className="border-b bg-muted/20">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-xl font-bold font-mono">
                {degree.degreeCode || degree.id}
              </CardTitle>
              <CardDescription>
                Issued degree record details
              </CardDescription>
            </div>
            <StatusBadge status={degree.status} />
          </div>
        </CardHeader>

        <CardContent className="pt-6 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <span className="text-xs text-muted-foreground uppercase font-semibold">
                Student Name / ID
              </span>
              <p className="text-sm font-medium mt-1">
                {degree.studentName || degree.studentId}
              </p>
            </div>

            <div>
              <span className="text-xs text-muted-foreground uppercase font-semibold">
                Student UUID
              </span>
              <p className="text-sm font-mono mt-1 text-muted-foreground">
                {degree.studentId}
              </p>
            </div>

            <div>
              <span className="text-xs text-muted-foreground uppercase font-semibold">
                Major
              </span>
              <p className="text-sm font-medium mt-1">{degree.major}</p>
            </div>

            <div>
              <span className="text-xs text-muted-foreground uppercase font-semibold">
                Classification
              </span>
              <p className="text-sm font-medium mt-1">{degree.classification}</p>
            </div>

            <div>
              <span className="text-xs text-muted-foreground uppercase font-semibold">
                Issued Date
              </span>
              <p className="text-sm font-medium mt-1">
                {new Date(degree.issuedAt).toLocaleDateString()}
              </p>
            </div>

            <div>
              <span className="text-xs text-muted-foreground uppercase font-semibold">
                Record Created
              </span>
              <p className="text-sm font-medium mt-1">
                {new Date(degree.createdAt).toLocaleString()}
              </p>
            </div>
          </div>

          {/* Blockchain information section */}
          <div className="border-t pt-4 mt-6">
            <h4 className="text-xs uppercase font-semibold text-muted-foreground mb-3">
              Blockchain Anchoring Information
            </h4>
            {degree.txHash ? (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm font-mono bg-muted/40 p-3 rounded border">
                <div>
                  <span className="text-xs text-muted-foreground block">Transaction Hash</span>
                  <span className="truncate block text-xs" title={degree.txHash}>
                    {degree.txHash}
                  </span>
                </div>
                <div>
                  <span className="text-xs text-muted-foreground block">Block Number</span>
                  <span className="text-xs">{degree.blockNumber ?? 'N/A'}</span>
                </div>
              </div>
            ) : (
              <p className="text-xs text-muted-foreground italic">
                Transaction proof is pending off-chain batch processing.
              </p>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
