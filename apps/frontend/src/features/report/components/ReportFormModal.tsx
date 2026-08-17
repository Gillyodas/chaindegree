import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
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
import { FileUpload } from '@/shared/components/FileUpload';
import { submitReportSchema, type SubmitReportFormValues } from '../report.schema';
import { useSubmitReportMutation } from '../hooks/useSubmitReport';

export interface ReportFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  degreeId: string;
  degreeCode?: string;
  onSuccess?: (reportId: string) => void;
}

export function ReportFormModal({
  isOpen,
  onClose,
  degreeId,
  degreeCode,
  onSuccess,
}: ReportFormModalProps) {
  const submitMutation = useSubmitReportMutation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    setValue,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SubmitReportFormValues>({
    resolver: zodResolver(submitReportSchema),
    defaultValues: {
      degreeId,
      reportType: 'Administrative_Error',
      description: '',
      evidenceFile: undefined as unknown as File,
    },
  });

  const handleClose = () => {
    reset({
      degreeId,
      reportType: 'Administrative_Error',
      description: '',
      evidenceFile: undefined as unknown as File,
    });
    setServerError(null);
    onClose();
  };

  const onSubmit = async (values: SubmitReportFormValues) => {
    setServerError(null);
    try {
      const response = await submitMutation.mutateAsync({
        degreeId: values.degreeId,
        reportType: values.reportType,
        description: values.description,
        evidenceFile: values.evidenceFile,
      });

      if (onSuccess) {
        onSuccess(response.reportId);
      }
      handleClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setServerError(err.message);
      } else {
        setServerError('An unexpected error occurred while submitting the report.');
      }
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="text-amber-600 dark:text-amber-400">
            Report Degree Issue / Fraud
          </DialogTitle>
          <DialogDescription>
            Submit an official dispute or fraud report for Degree:{' '}
            <span className="font-semibold text-foreground">
              {degreeCode || degreeId}
            </span>
            . Physical evidence is required for audit verification.
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
            <label className="text-sm font-medium" htmlFor="reportType">
              Report Type <span className="text-destructive">*</span>
            </label>
            <Controller
              control={control}
              name="reportType"
              render={({ field }) => (
                <Select
                  value={field.value}
                  onValueChange={field.onChange}
                  disabled={isSubmitting}
                >
                  <SelectTrigger id="reportType" className="w-full">
                    <SelectValue placeholder="Select report type" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Administrative_Error">
                      Administrative Error (Typo, Date, Classification mismatch)
                    </SelectItem>
                    <SelectItem value="Fraudulent_Data">
                      Fraudulent Data (Fabrication, Illegal issuance, Tampered record)
                    </SelectItem>
                  </SelectContent>
                </Select>
              )}
            />
            {errors.reportType && (
              <p className="text-xs text-destructive">{errors.reportType.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <label className="text-sm font-medium" htmlFor="description">
              Detailed Description <span className="text-destructive">*</span>
            </label>
            <Controller
              control={control}
              name="description"
              render={({ field }) => (
                <textarea
                  {...field}
                  id="description"
                  rows={4}
                  placeholder="Explain the specific issue or discrepancy found on this degree..."
                  disabled={isSubmitting}
                  className="w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                />
              )}
            />
            {errors.description && (
              <p className="text-xs text-destructive">{errors.description.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <label className="text-sm font-medium">
              Physical Evidence File (PDF, PNG, JPG ≤ 5MB){' '}
              <span className="text-destructive">*</span>
            </label>
            <Controller
              control={control}
              name="evidenceFile"
              render={({ field }) => (
                <FileUpload
                  maxSizeMB={5}
                  accept=".pdf,.png,.jpg,.jpeg"
                  selectedFile={field.value}
                  onFileSelect={(file) => {
                    setValue('evidenceFile', file as File, {
                      shouldValidate: true,
                    });
                  }}
                  error={errors.evidenceFile?.message}
                />
              )}
            />
          </div>

          <DialogFooter className="pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              className="bg-amber-600 hover:bg-amber-700 text-white"
            >
              {isSubmitting ? 'Submitting Report...' : 'Submit Report'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
