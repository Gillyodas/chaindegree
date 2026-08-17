import { useState } from 'react';
import { useParams, Link } from 'react-router';
import { ArrowLeft, Edit3, ShieldOff, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';

import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { EmptyState } from '@/shared/components/EmptyState';
import { ErrorState } from '@/shared/components/ErrorState';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import { HttpError } from '@/shared/api/http';
import { useAuth } from '@/app/providers/AuthProvider';
import { useDegreeDetailQuery } from '../hooks/useDegreeQueries';
import { UpdateDegreeModal } from '../components/UpdateDegreeModal';
import { RevokeDegreeDialog } from '../components/RevokeDegreeDialog';
import { ReportFormModal } from '@/features/report/components/ReportFormModal';

export function DegreeDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { currentUser } = useAuth();
  const { data: degree, isLoading, error, refetch } = useDegreeDetailQuery(id || '');

  const [isUpdateOpen, setIsUpdateOpen] = useState(false);
  const [isRevokeOpen, setIsRevokeOpen] = useState(false);
  const [isReportOpen, setIsReportOpen] = useState(false);

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

  // State Transition UX Hints: Only Confirmed or Pending_Confirmation allow Update/Revoke
  const canUpdateOrRevoke =
    (currentUser?.role === 'Registrar') &&
    (degree.status === 'Confirmed' || degree.status === 'Pending_Confirmation');

  // RBAC for reporting: Recruiter can report any degree, Student can report their own degree
  const canReport =
    currentUser?.role === 'Recruiter' ||
    (currentUser?.role === 'Student' && currentUser.id === degree.studentId);

  const handleUpdateSuccess = (msg: string) => {
    toast.success(msg);
    refetch();
  };

  const handleRevokeSuccess = (msg: string) => {
    toast.success(msg);
    refetch();
  };

  const handleConflict = () => {
    toast.error('The degree state has changed. Please refresh and try again.');
    refetch();
  };

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      <div className="flex items-center justify-between">
        <Link to="/degrees">
          <Button variant="ghost" size="sm" className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Back to Degrees
          </Button>
        </Link>

        <div className="flex items-center space-x-2">
          {currentUser?.role === 'Registrar' && (
            <>
              <Button
                variant="outline"
                size="sm"
                disabled={!canUpdateOrRevoke}
                onClick={() => setIsUpdateOpen(true)}
                title={
                  canUpdateOrRevoke
                    ? 'Update Degree Academic Details'
                    : 'Degree status does not permit update'
                }
              >
                <Edit3 className="h-4 w-4 mr-1.5" />
                Update
              </Button>

              <Button
                variant="outline"
                size="sm"
                disabled={!canUpdateOrRevoke}
                onClick={() => setIsRevokeOpen(true)}
                title={
                  canUpdateOrRevoke
                    ? 'Revoke Degree'
                    : 'Degree status does not permit revocation'
                }
                className="text-destructive border-destructive/30 hover:bg-destructive/10"
              >
                <ShieldOff className="h-4 w-4 mr-1.5" />
                Revoke
              </Button>
            </>
          )}

          {canReport && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsReportOpen(true)}
              title="Report issue or fraudulent data on this degree"
              className="text-amber-600 border-amber-300 hover:bg-amber-50 dark:hover:bg-amber-950/40"
            >
              <AlertTriangle className="h-4 w-4 mr-1.5" />
              Report Issue / Fraud
            </Button>
          )}
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
                Issued degree record details (Version {degree.currentVersion})
              </CardDescription>
            </div>
            <StatusBadge status={degree.status} />
          </div>
        </CardHeader>

        <CardContent className="pt-6 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <span className="text-xs text-muted-foreground uppercase font-semibold">
                Student Full Name
              </span>
              <p className="text-sm font-medium mt-1">
                {degree.studentFullName || degree.studentId}
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

          <div className="border-t pt-4 mt-6">
            <h4 className="text-xs uppercase font-semibold text-muted-foreground mb-3">
              Blockchain Anchoring Information
            </h4>
            {degree.txHashBlockchain ? (
              <div className="grid grid-cols-1 gap-4 text-sm font-mono bg-muted/40 p-3 rounded border">
                <div>
                  <span className="text-xs text-muted-foreground block">Transaction Hash</span>
                  <span className="truncate block text-xs" title={degree.txHashBlockchain}>
                    {degree.txHashBlockchain}
                  </span>
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

      {/* Modals */}
      <UpdateDegreeModal
        isOpen={isUpdateOpen}
        onClose={() => setIsUpdateOpen(false)}
        degree={degree}
        onSuccess={handleUpdateSuccess}
        onConflict={handleConflict}
      />

      <RevokeDegreeDialog
        isOpen={isRevokeOpen}
        onClose={() => setIsRevokeOpen(false)}
        degree={degree}
        onSuccess={handleRevokeSuccess}
        onConflict={handleConflict}
      />

      <ReportFormModal
        isOpen={isReportOpen}
        onClose={() => setIsReportOpen(false)}
        degreeId={degree.id}
        degreeCode={degree.degreeCode}
        onSuccess={() => {
          refetch();
        }}
      />
    </div>
  );
}
