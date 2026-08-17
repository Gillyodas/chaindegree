import { useState } from 'react';
import { Download, CheckCircle2, XCircle, FileText, AlertCircle } from 'lucide-react';
import { Button } from '@/shared/components/ui/button';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { ConfirmDialog } from '@/shared/components/ConfirmDialog';
import { RejectReportModal } from './RejectReportModal';
import { useApproveReportMutation } from '../hooks/useApproveReport';
import { useDownloadEvidence } from '../hooks/useDownloadEvidence';
import type { ReportListItem } from '../report.types';
import { formatDate } from '@/shared/lib/date';

export interface ReportListTableProps {
  reports: ReportListItem[];
  onActionComplete?: () => void;
}

export function ReportListTable({ reports, onActionComplete }: ReportListTableProps) {
  const approveMutation = useApproveReportMutation();
  const { downloadEvidence, isDownloading, downloadingId } = useDownloadEvidence();

  const [approveTarget, setApproveTarget] = useState<ReportListItem | null>(null);
  const [rejectTarget, setRejectTarget] = useState<ReportListItem | null>(null);

  const handleConfirmApprove = async () => {
    if (!approveTarget) return;
    try {
      await approveMutation.mutateAsync(approveTarget.id);
      if (onActionComplete) {
        onActionComplete();
      }
    } finally {
      setApproveTarget(null);
    }
  };

  return (
    <div className="rounded-md border bg-card">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/40 text-left font-medium text-muted-foreground">
              <th className="py-3 px-4">Report ID</th>
              <th className="py-3 px-4">Degree Code</th>
              <th className="py-3 px-4">Reporter</th>
              <th className="py-3 px-4">Type</th>
              <th className="py-3 px-4 max-w-xs">Description</th>
              <th className="py-3 px-4">Status</th>
              <th className="py-3 px-4">Submitted</th>
              <th className="py-3 px-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {reports.map((report) => {
              const isPending = report.status === 'Pending_Review';
              const isItemDownloading = isDownloading && downloadingId === report.id;

              return (
                <tr
                  key={report.id}
                  className="hover:bg-muted/30 transition-colors"
                >
                  <td className="py-3 px-4 font-mono text-xs text-muted-foreground font-semibold">
                    <span title={report.id}>
                      {report.id.length > 8 ? `${report.id.substring(0, 8)}...` : report.id}
                    </span>
                  </td>

                  <td className="py-3 px-4 font-mono font-medium">
                    {report.degreeCode || report.degreeId}
                  </td>

                  <td className="py-3 px-4">
                    <div className="text-xs">
                      <span className="font-medium">{report.reporterRole}</span>
                      <p className="text-muted-foreground truncate max-w-[100px]" title={report.reporterId}>
                        {report.reporterId}
                      </p>
                    </div>
                  </td>

                  <td className="py-3 px-4">
                    {report.reportType === 'Fraudulent_Data' ? (
                      <span className="inline-flex items-center gap-1 text-xs font-semibold px-2 py-0.5 rounded-full bg-rose-100 text-rose-800 dark:bg-rose-950/60 dark:text-rose-300">
                        <AlertCircle className="h-3 w-3" />
                        Fraudulent
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-xs font-semibold px-2 py-0.5 rounded-full bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300">
                        <FileText className="h-3 w-3" />
                        Administrative
                      </span>
                    )}
                  </td>

                  <td className="py-3 px-4 max-w-xs truncate" title={report.description}>
                    <span className="truncate block text-xs text-muted-foreground">
                      {report.description}
                    </span>
                  </td>

                  <td className="py-3 px-4">
                    <StatusBadge status={report.status} />
                  </td>

                  <td className="py-3 px-4 text-xs text-muted-foreground whitespace-nowrap">
                    {formatDate(report.createdAt)}
                  </td>

                  <td className="py-3 px-4 text-right">
                    <div className="flex items-center justify-end space-x-1.5">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => downloadEvidence(report.id, `evidence-${report.id}.pdf`)}
                        disabled={isItemDownloading}
                        title="Download Physical Evidence File"
                        className="h-8 px-2 text-xs"
                      >
                        <Download className="h-3.5 w-3.5 mr-1" />
                        {isItemDownloading ? 'Downloading...' : 'Evidence'}
                      </Button>

                      {isPending && (
                        <>
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setApproveTarget(report)}
                            title="Approve Report"
                            className="h-8 px-2 text-xs border-emerald-300 text-emerald-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/40"
                          >
                            <CheckCircle2 className="h-3.5 w-3.5 mr-1" />
                            Approve
                          </Button>

                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setRejectTarget(report)}
                            title="Reject Report"
                            className="h-8 px-2 text-xs border-rose-300 text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/40"
                          >
                            <XCircle className="h-3.5 w-3.5 mr-1" />
                            Reject
                          </Button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Confirm Approve Dialog */}
      <ConfirmDialog
        open={approveTarget !== null}
        onOpenChange={(open) => !open && setApproveTarget(null)}
        title="Approve Dispute / Fraud Report"
        description={`Are you sure you want to approve this report for Degree: ${
          approveTarget?.degreeCode || approveTarget?.degreeId
        }? This will trigger revocation workflows and emit domain events to the Reputation penalty system.`}
        confirmLabel="Confirm Approval"
        variant="default"
        isLoading={approveMutation.isPending}
        onConfirm={handleConfirmApprove}
      />

      {/* Reject Modal */}
      {rejectTarget && (
        <RejectReportModal
          isOpen={rejectTarget !== null}
          onClose={() => setRejectTarget(null)}
          reportId={rejectTarget.id}
          degreeCode={rejectTarget.degreeCode}
          onSuccess={() => {
            if (onActionComplete) {
              onActionComplete();
            }
          }}
        />
      )}
    </div>
  );
}
