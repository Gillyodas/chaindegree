import { useState, useEffect } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AxiosError } from 'axios';

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/shared/components/ui/dialog';
import { Button } from '@/shared/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select';
import { useRevokeDegreeMutation } from '../hooks/useDegreeMutations';
import type { DegreeDetail } from '../degree.types';
import { DEGREE_REVOCATION_REASONS } from '../degree.types';

const revokeDegreeSchema = z.object({
  reasonCode: z.string().min(1, 'Reason code is required'),
});

export type RevokeDegreeFormValues = z.infer<typeof revokeDegreeSchema>;

export interface RevokeDegreeDialogProps {
  isOpen: boolean;
  onClose: () => void;
  degree: DegreeDetail;
  onSuccess?: (message: string) => void;
  onConflict?: () => void;
}

export function RevokeDegreeDialog({
  isOpen,
  onClose,
  degree,
  onSuccess,
  onConflict,
}: RevokeDegreeDialogProps) {
  const revokeMutation = useRevokeDegreeMutation();
  const [serverError, setServerError] = useState<string | null>(null);
  const [isAmbiguous, setIsAmbiguous] = useState<boolean>(false);

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RevokeDegreeFormValues>({
    resolver: zodResolver(revokeDegreeSchema),
    defaultValues: {
      reasonCode: 'R-01',
    },
  });

  useEffect(() => {
    if (isOpen) {
      reset({
        reasonCode: 'R-01',
      });
      setServerError(null);
      setIsAmbiguous(false);
    }
  }, [isOpen, reset]);

  const onSubmit = async (values: RevokeDegreeFormValues) => {
    setServerError(null);
    setIsAmbiguous(false);

    const idempotencyKey = crypto.randomUUID();

    try {
      const response = await revokeMutation.mutateAsync({
        id: degree.id,
        data: {
          reasonCode: values.reasonCode,
        },
        idempotencyKey,
      });

      const message =
        response.currentStatus === 'Revoked' || response.isShortcut
          ? 'Degree revoked successfully.'
          : 'Revocation request accepted. Processing continues in the background.';

      if (onSuccess) {
        onSuccess(message);
      }
      onClose();
    } catch (err: unknown) {
      const isAxiosError = err instanceof AxiosError;
      const status = isAxiosError ? err.response?.status : undefined;

      if (status === 409) {
        const conflictMsg = 'The degree state has changed. Please refresh and try again.';
        setServerError(conflictMsg);
        if (onConflict) {
          onConflict();
        }
        onClose();
      } else if (status === 400 || status === 422) {
        const detail = (err as AxiosError<{ detail?: string }>).response?.data?.detail;
        setServerError(detail || 'Invalid revocation request.');
      } else if (!status || status >= 500) {
        setIsAmbiguous(true);
        setServerError('Unable to determine the current operation result. The degree is being rechecked.');
      } else {
        setServerError('An unexpected error occurred.');
      }
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="text-rose-600 dark:text-rose-400">
            Revoke Degree
          </DialogTitle>
          <DialogDescription>
            Are you sure you want to revoke Degree Code:{' '}
            <span className="font-semibold text-foreground">{degree.degreeCode}</span>?
            This operation is legally binding and will be permanently recorded.
          </DialogDescription>
        </DialogHeader>

        {serverError && (
          <div
            role="alert"
            className={`p-3 rounded text-sm font-medium ${
              isAmbiguous
                ? 'bg-amber-100 text-amber-800 border border-amber-300 dark:bg-amber-950 dark:text-amber-300'
                : 'bg-rose-100 text-rose-800 border border-rose-300 dark:bg-rose-950 dark:text-rose-300'
            }`}
          >
            {serverError}
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="reasonCode">
              Reason Code for Revocation
            </label>
            <Controller
              control={control}
              name="reasonCode"
              render={({ field }) => (
                <Select
                  value={field.value}
                  onValueChange={field.onChange}
                  disabled={isSubmitting || isAmbiguous}
                >
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Select reason code" />
                  </SelectTrigger>
                  <SelectContent>
                    {DEGREE_REVOCATION_REASONS.map((opt) => (
                      <SelectItem key={opt.code} value={opt.code}>
                        {opt.description}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.reasonCode && (
              <p className="text-xs text-rose-600">{errors.reasonCode.message}</p>
            )}
          </div>

          <DialogFooter className="pt-4">
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
              disabled={isSubmitting || isAmbiguous}
            >
              {isSubmitting ? 'Revoking...' : 'Confirm Revocation'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
