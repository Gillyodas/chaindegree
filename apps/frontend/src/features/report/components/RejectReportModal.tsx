import { useEffect, useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/shared/components/ui/dialog';
import { Button } from '@/shared/components/ui/button';
import { useRejectReportMutation } from '../hooks/useRejectReport';

const rejectReportSchema = z.object({
  reason: z
    .string({ required_error: 'Rejection reason is required.' })
    .trim()
    .min(5, 'Rejection reason must be at least 5 characters long.')
    .max(1000, 'Rejection reason cannot exceed 1000 characters.'),
});

type RejectReportFormValues = z.infer<typeof rejectReportSchema>;

export interface RejectReportModalProps {
  isOpen: boolean;
  onClose: () => void;
  reportId: string;
  degreeCode?: string;
  onSuccess?: () => void;
}

export function RejectReportModal({
  isOpen,
  onClose,
  reportId,
  degreeCode,
  onSuccess,
}: RejectReportModalProps) {
  const rejectMutation = useRejectReportMutation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RejectReportFormValues>({
    resolver: zodResolver(rejectReportSchema),
    defaultValues: {
      reason: '',
    },
  });

  useEffect(() => {
    if (isOpen) {
      reset({ reason: '' });
      setServerError(null);
    }
  }, [isOpen, reset]);

  const onSubmit = async (values: RejectReportFormValues) => {
    setServerError(null);
    try {
      await rejectMutation.mutateAsync({
        id: reportId,
        reason: values.reason,
      });

      if (onSuccess) {
        onSuccess();
      }
      onClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('An unexpected error occurred while rejecting the report.');
      }
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-destructive">
            Reject Report
          </DialogTitle>
          <DialogDescription>
            Provide a clear and justified reason for rejecting Report ID:{' '}
            <span className="font-semibold text-foreground font-mono">{reportId}</span>
            {degreeCode && (
              <>
                {' '}
                for Degree:{' '}
                <span className="font-semibold text-foreground font-mono">
                  {degreeCode}
                </span>
              </>
            )}
            .
          </DialogDescription>
        </DialogHeader>

        {serverError && (
          <div
            role="alert"
            className="p-3 rounded text-sm font-medium bg-rose-100 text-rose-800 border border-rose-300 dark:bg-rose-950 dark:text-rose-300"
          >
            {serverError}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-1.5">
            <label className="text-sm font-medium" htmlFor="rejectReason">
              Rejection Reason <span className="text-destructive">*</span>
            </label>
            <Controller
              control={control}
              name="reason"
              render={({ field }) => (
                <textarea
                  {...field}
                  id="rejectReason"
                  rows={4}
                  placeholder="Explain why this report is unsubstantiated or rejected..."
                  disabled={isSubmitting}
                  className="w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                />
              )}
            />
            {errors.reason && (
              <p className="text-xs text-destructive">{errors.reason.message}</p>
            )}
          </div>

          <DialogFooter className="pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Rejecting...' : 'Confirm Rejection'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
