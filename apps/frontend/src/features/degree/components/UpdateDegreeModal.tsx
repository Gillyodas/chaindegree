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
import { Input } from '@/shared/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select';
import { useUpdateDegreeMutation } from '../hooks/useDegreeMutations';
import type { DegreeDetail } from '../degree.types';
import { DEGREE_UPDATE_REASONS } from '../degree.types';

const updateDegreeSchema = z.object({
  major: z.string().min(1, 'Major is required'),
  classification: z.string().min(1, 'Classification is required'),
  reasonCode: z.string().min(1, 'Reason code is required'),
});

export type UpdateDegreeFormValues = z.infer<typeof updateDegreeSchema>;

export interface UpdateDegreeModalProps {
  isOpen: boolean;
  onClose: () => void;
  degree: DegreeDetail;
  onSuccess?: (message: string) => void;
  onConflict?: () => void;
}

export function UpdateDegreeModal({
  isOpen,
  onClose,
  degree,
  onSuccess,
  onConflict,
}: UpdateDegreeModalProps) {
  const updateMutation = useUpdateDegreeMutation();
  const [serverError, setServerError] = useState<string | null>(null);
  const [isAmbiguous, setIsAmbiguous] = useState<boolean>(false);

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<UpdateDegreeFormValues>({
    resolver: zodResolver(updateDegreeSchema),
    defaultValues: {
      major: degree.major,
      classification: degree.classification,
      reasonCode: 'S-01',
    },
  });

  useEffect(() => {
    if (isOpen) {
      reset({
        major: degree.major,
        classification: degree.classification,
        reasonCode: 'S-01',
      });
      setServerError(null);
      setIsAmbiguous(false);
    }
  }, [isOpen, degree, reset]);

  const onSubmit = async (values: UpdateDegreeFormValues) => {
    setServerError(null);
    setIsAmbiguous(false);

    const idempotencyKey = crypto.randomUUID();

    try {
      const response = await updateMutation.mutateAsync({
        id: degree.id,
        data: {
          major: values.major,
          classification: values.classification,
          reasonCode: values.reasonCode,
        },
        idempotencyKey,
      });

      const message = response.isShortcut
        ? 'Degree details updated directly.'
        : 'Degree update request accepted. Processing continues in the background.';

      if (onSuccess) {
        onSuccess(message);
      }
      onClose();
    } catch (err: unknown) {
      const isAxiosError = err instanceof AxiosError;
      const status = isAxiosError ? err.response?.status : undefined;

      if (status === 409) {
        // State Conflict
        const conflictMsg = 'The degree state has changed. Please refresh and try again.';
        setServerError(conflictMsg);
        if (onConflict) {
          onConflict();
        }
        onClose();
      } else if (status === 400 || status === 422) {
        const detail = (err as AxiosError<{ detail?: string }>).response?.data?.detail;
        setServerError(detail || 'Invalid validation request.');
      } else if (!status || status >= 500) {
        // Ambiguous Outcome (Timeout / Network / Server Error)
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
          <DialogTitle>Update Degree Information</DialogTitle>
          <DialogDescription>
            Modify academic details for Degree Code: <span className="font-semibold">{degree.degreeCode}</span>
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
            <label className="text-sm font-medium" htmlFor="major">
              Major
            </label>
            <Input
              id="major"
              {...register('major')}
              disabled={isSubmitting || isAmbiguous}
              placeholder="e.g. Computer Science"
            />
            {errors.major && (
              <p className="text-xs text-rose-600">{errors.major.message}</p>
            )}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="classification">
              Classification
            </label>
            <Input
              id="classification"
              {...register('classification')}
              disabled={isSubmitting || isAmbiguous}
              placeholder="e.g. Excellent / High Distinction"
            />
            {errors.classification && (
              <p className="text-xs text-rose-600">{errors.classification.message}</p>
            )}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium" htmlFor="reasonCode">
              Reason Code
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
                    {DEGREE_UPDATE_REASONS.map((opt) => (
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
              disabled={isSubmitting || isAmbiguous}
            >
              {isSubmitting ? 'Updating...' : 'Confirm Update'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
