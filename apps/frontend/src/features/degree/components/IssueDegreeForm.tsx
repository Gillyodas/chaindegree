import { useRef, useState } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from 'sonner';
import { Plus, Trash2, Send, AlertCircle } from 'lucide-react';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { useIssueDegreesMutation } from '../hooks/useIssueDegrees';

export const issueDegreeItemSchema = z.object({
  studentId: z
    .string()
    .min(1, 'Student ID is required')
    .regex(
      /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/,
      'Must be a valid UUID',
    ),
  major: z.string().min(1, 'Major is required'),
  classification: z.string().min(1, 'Classification is required'),
  issuedAt: z.string().min(1, 'Issue date is required'),
});

export const issueDegreeFormSchema = z.object({
  degrees: z.array(issueDegreeItemSchema).min(1, 'At least one degree is required'),
});

export type IssueDegreeFormData = z.infer<typeof issueDegreeFormSchema>;

export interface RowError {
  studentId: string;
  major: string;
  reason: string;
}

export function IssueDegreeForm() {
  const [rowErrors, setRowErrors] = useState<RowError[]>([]);
  const idempotencyKeyRef = useRef<string | null>(null);

  const issueMutation = useIssueDegreesMutation();

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<IssueDegreeFormData>({
    resolver: zodResolver(issueDegreeFormSchema),
    defaultValues: {
      degrees: [
        {
          studentId: '',
          major: '',
          classification: '',
          issuedAt: new Date().toISOString().split('T')[0] ?? '',
        },
      ],
    },
  });

  const { fields, append, remove, replace } = useFieldArray({
    control,
    name: 'degrees',
  });

  const onSubmit = (data: IssueDegreeFormData) => {
    setRowErrors([]);

    // Generate new key for a new logical submission
    idempotencyKeyRef.current = crypto.randomUUID();

    const formattedRequest = {
      degrees: data.degrees.map((d) => ({
        ...d,
        issuedAt: new Date(d.issuedAt).toISOString(),
      })),
    };

    issueMutation.mutate(
      {
        data: formattedRequest,
        idempotencyKey: idempotencyKeyRef.current,
      },
      {
        onSuccess: (response) => {
          const failures = response.failures || [];
          const acceptedCount = response.acceptedCount || 0;

          if (failures.length === 0) {
            toast.success(
              `Successfully submitted ${acceptedCount} degree(s). The system is processing verification in the background.`,
            );
            setRowErrors([]);
            reset({
              degrees: [
                {
                  studentId: '',
                  major: '',
                  classification: '',
                  issuedAt: new Date().toISOString().split('T')[0] ?? '',
                },
              ],
            });
          } else if (acceptedCount > 0) {
            toast.warning(
              `Submitted ${acceptedCount} degree(s) successfully. ${failures.length} degree(s) could not be processed — see errors below.`,
            );
            setRowErrors(failures);

            const failedKeys = new Set(
              failures.map((f) => `${f.studentId.toLowerCase()}:${f.major.toLowerCase()}`),
            );
            const remainingRows = data.degrees.filter((item) =>
              failedKeys.has(`${item.studentId.toLowerCase()}:${item.major.toLowerCase()}`),
            );

            if (remainingRows.length > 0) {
              replace(remainingRows);
            }
          } else {
            toast.error('All degree issuance requests were rejected.');
            setRowErrors(failures);
          }
        },
        onError: (error) => {
          toast.error(error.message || 'Failed to submit degree issuance request.');
        },
      },
    );
  };

  const getRowErrorMessage = (studentId: string, major: string) => {
    if (!studentId || !major) return null;
    const match = rowErrors.find(
      (e) =>
        e.studentId.toLowerCase() === studentId.toLowerCase() &&
        e.major.toLowerCase() === major.toLowerCase(),
    );
    return match ? match.reason : null;
  };

  return (
    <Card className="w-full">
      <CardHeader>
        <CardTitle className="text-xl font-bold">Issue Degrees</CardTitle>
        <CardDescription>
          Enter degree details below to issue degrees to students. You can add multiple degrees at once.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="space-y-4">
            {fields.map((field, index) => {
              const studentIdVal = field.studentId;
              const majorVal = field.major;
              const inlineErrorMsg = getRowErrorMessage(studentIdVal, majorVal);

              return (
                <div
                  key={field.id}
                  className={`p-4 border rounded-lg space-y-3 transition-colors ${
                    inlineErrorMsg ? 'border-destructive bg-destructive/5' : 'border-border'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-semibold text-muted-foreground">
                      Degree #{index + 1}
                    </span>
                    {fields.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => remove(index)}
                        className="text-destructive hover:text-destructive hover:bg-destructive/10"
                      >
                        <Trash2 className="h-4 w-4 mr-1" />
                        Remove
                      </Button>
                    )}
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                    <div>
                      <label className="block text-xs font-medium mb-1">
                        Student ID (UUID) <span className="text-destructive">*</span>
                      </label>
                      <Input
                        placeholder="e.g. 550e8400-e29b-41d4-a716-446655440000"
                        {...register(`degrees.${index}.studentId`)}
                      />
                      {errors.degrees?.[index]?.studentId && (
                        <p className="text-xs text-destructive mt-1">
                          {errors.degrees[index]?.studentId?.message}
                        </p>
                      )}
                    </div>

                    <div>
                      <label className="block text-xs font-medium mb-1">
                        Major <span className="text-destructive">*</span>
                      </label>
                      <Input
                        placeholder="e.g. Software Engineering"
                        {...register(`degrees.${index}.major`)}
                      />
                      {errors.degrees?.[index]?.major && (
                        <p className="text-xs text-destructive mt-1">
                          {errors.degrees[index]?.major?.message}
                        </p>
                      )}
                    </div>

                    <div>
                      <label className="block text-xs font-medium mb-1">
                        Classification <span className="text-destructive">*</span>
                      </label>
                      <Input
                        placeholder="e.g. Excellent / Giỏi"
                        {...register(`degrees.${index}.classification`)}
                      />
                      {errors.degrees?.[index]?.classification && (
                        <p className="text-xs text-destructive mt-1">
                          {errors.degrees[index]?.classification?.message}
                        </p>
                      )}
                    </div>

                    <div>
                      <label className="block text-xs font-medium mb-1">
                        Issued Date <span className="text-destructive">*</span>
                      </label>
                      <Input
                        type="date"
                        {...register(`degrees.${index}.issuedAt`)}
                      />
                      {errors.degrees?.[index]?.issuedAt && (
                        <p className="text-xs text-destructive mt-1">
                          {errors.degrees[index]?.issuedAt?.message}
                        </p>
                      )}
                    </div>
                  </div>

                  {inlineErrorMsg && (
                    <div className="flex items-center text-xs text-destructive bg-destructive/10 p-2 rounded">
                      <AlertCircle className="h-4 w-4 mr-1.5 flex-shrink-0" />
                      <span>{inlineErrorMsg}</span>
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          <div className="flex items-center justify-between pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                append({
                  studentId: '',
                  major: '',
                  classification: '',
                  issuedAt: new Date().toISOString().split('T')[0] ?? '',
                })
              }
            >
              <Plus className="h-4 w-4 mr-1" />
              Add Degree
            </Button>

            <Button
              type="submit"
              disabled={isSubmitting || issueMutation.isPending}
            >
              <Send className="h-4 w-4 mr-1.5" />
              {issueMutation.isPending ? 'Submitting...' : 'Submit Degrees'}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
