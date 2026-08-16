import { useEffect, useRef, type ChangeEvent } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Search, Loader2, AlertTriangle, AlertCircle } from 'lucide-react';
import { Input } from '@/shared/components/ui/input';
import { Button } from '@/shared/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select';
import { verificationSchema, type VerificationFormData } from '../verification.schema';
import type { DegreeVersionItem } from '../verification.types';

export interface VerificationFormProps {
  onSubmit: (data: { degreeCode: string; version?: number | null }) => void;
  isSubmitting: boolean;
  inputError?: string | null;
  onDegreeCodeChange?: () => void;
  onVersionChange?: () => void;
  degreeCode: string;
  onDegreeCodeInputChange: (code: string) => void;
  versions: DegreeVersionItem[];
  versionsLoading: boolean;
  degreeNotFound: boolean;
}

export function VerificationForm({
  onSubmit,
  isSubmitting,
  inputError,
  onDegreeCodeChange,
  onVersionChange,
  degreeCode,
  onDegreeCodeInputChange,
  versions,
  versionsLoading,
  degreeNotFound,
}: VerificationFormProps) {
  const inputRef = useRef<HTMLInputElement>(null);

  const {
    register,
    handleSubmit,
    control,
    setValue,
    formState: { errors },
  } = useForm<VerificationFormData>({
    resolver: zodResolver(verificationSchema),
    defaultValues: {
      degreeCode: degreeCode || '',
      version: undefined,
    },
  });

  // Auto-focus on degree code input on mount
  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  // Sync external degreeCode state with form
  useEffect(() => {
    setValue('degreeCode', degreeCode);
  }, [degreeCode, setValue]);

  const handleFormSubmit = (data: VerificationFormData) => {
    onSubmit({
      degreeCode: data.degreeCode.trim(),
      version: data.version ?? null,
    });
  };

  const isSelectDisabled =
    !degreeCode.trim() || versionsLoading || degreeNotFound || isSubmitting;

  return (
    <form
      onSubmit={handleSubmit(handleFormSubmit)}
      className="space-y-5"
      noValidate
      aria-label="Degree Verification Form"
    >
      {/* Degree Code Field */}
      <div className="space-y-2">
        <label
          htmlFor="degree-code-input"
          className="block text-sm font-semibold text-foreground"
        >
          Degree Code <span className="text-destructive">*</span>
        </label>
        <div className="relative">
          <Input
            id="degree-code-input"
            {...register('degreeCode', {
              onChange: (e: ChangeEvent<HTMLInputElement>) => {
                onDegreeCodeInputChange(e.target.value);
                onDegreeCodeChange?.();
              },
            })}
            ref={(e) => {
              register('degreeCode').ref(e);
              inputRef.current = e;
            }}
            placeholder="Enter degree code (e.g., DEG-2026-000001)"
            className="font-mono text-base tracking-wide uppercase pr-10"
            disabled={isSubmitting}
            autoComplete="off"
            spellCheck={false}
          />
          {versionsLoading && (
            <div className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin text-primary" />
            </div>
          )}
        </div>

        {errors.degreeCode && (
          <p className="text-xs text-destructive font-medium flex items-center gap-1 mt-1">
            <AlertCircle className="h-3.5 w-3.5" />
            {errors.degreeCode.message}
          </p>
        )}

        {/* Fail-Fast Inline Warning: Degree Not Found */}
        {degreeNotFound && (
          <div
            role="alert"
            className="flex items-center gap-2 p-3 rounded-lg bg-amber-50 border border-amber-200 text-amber-900 dark:bg-amber-950/30 dark:border-amber-900/50 dark:text-amber-200 text-sm font-medium animate-in fade-in duration-200"
          >
            <AlertTriangle className="h-4 w-4 text-amber-600 dark:text-amber-400 shrink-0" />
            <span>No degree found with this code. Please check and try again.</span>
          </div>
        )}

        {/* Inline Input Error from Backend (e.g., INVALID_SALT_FORMAT) */}
        {inputError && (
          <div
            role="alert"
            className="flex items-center gap-2 p-3 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-sm font-medium animate-in fade-in duration-200"
          >
            <AlertCircle className="h-4 w-4 shrink-0" />
            <span>{inputError}</span>
          </div>
        )}
      </div>

      {/* Version Combobox/Select Field */}
      <div className="space-y-2">
        <label
          htmlFor="version-select-trigger"
          className="block text-sm font-semibold text-foreground"
        >
          Version <span className="text-xs font-normal text-muted-foreground">(Optional)</span>
        </label>

        <Controller
          control={control}
          name="version"
          render={({ field }) => (
            <Select
              disabled={isSelectDisabled}
              value={field.value ? String(field.value) : 'latest'}
              onValueChange={(val) => {
                const numericVal = val === 'latest' ? undefined : Number(val);
                field.onChange(numericVal);
                onVersionChange?.();
              }}
            >
              <SelectTrigger id="version-select-trigger" className="w-full">
                <SelectValue
                  placeholder={
                    !degreeCode.trim()
                      ? 'Enter degree code first'
                      : versionsLoading
                        ? 'Loading versions...'
                        : degreeNotFound
                          ? 'Degree not found'
                          : 'Latest version'
                  }
                />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="latest">Latest version (default)</SelectItem>
                {versions.map((item) => (
                  <SelectItem key={item.version} value={String(item.version)}>
                    Version {item.version}
                    {item.isCurrent ? ' (Current)' : ''}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
        <p className="text-xs text-muted-foreground">
          By default, verifying checks the latest confirmed version on the blockchain.
        </p>
      </div>

      {/* Submit Button */}
      <Button
        type="submit"
        className="w-full h-11 text-base font-semibold transition-all"
        disabled={isSubmitting || !degreeCode.trim() || degreeNotFound}
      >
        {isSubmitting ? (
          <>
            <Loader2 className="mr-2 h-5 w-5 animate-spin" />
            Verifying Degree...
          </>
        ) : (
          <>
            <Search className="mr-2 h-5 w-5" />
            Verify Degree
          </>
        )}
      </Button>
    </form>
  );
}
