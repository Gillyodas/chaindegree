import React from 'react';
import { AlertCircle, RotateCcw, Clock } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Button } from '@/shared/components/ui/button';
import { Badge } from '@/shared/components/ui/badge';

export interface VerificationErrorProps {
  message: string;
  isRateLimited?: boolean;
  onRetry?: () => void;
}

export function VerificationError({ message, isRateLimited, onRetry }: VerificationErrorProps) {
  return (
    <Card
      className="border-2 border-destructive/50 bg-destructive/5 shadow-md animate-in fade-in zoom-in-95 duration-300"
      role="alert"
      aria-label="Verification error"
    >
      <CardHeader className="pb-3 border-b border-destructive/20">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-destructive/10 rounded-full text-destructive">
              {isRateLimited ? <Clock className="h-6 w-6" /> : <AlertCircle className="h-6 w-6" />}
            </div>
            <div>
              <CardTitle className="text-xl font-bold text-destructive">
                {isRateLimited ? 'Rate Limit Exceeded' : 'Verification Request Failed'}
              </CardTitle>
              <p className="text-xs text-destructive/80 mt-0.5">
                {isRateLimited
                  ? 'Too many verification attempts'
                  : 'Unable to complete the verification request'}
              </p>
            </div>
          </div>
          <Badge variant="destructive" className="font-semibold text-xs px-3 py-1">
            {isRateLimited ? 'Rate Limited' : 'Error'}
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="pt-5 space-y-4">
        <div className="p-4 rounded-lg bg-background border border-destructive/20 text-sm text-foreground space-y-2">
          <p className="font-medium text-destructive">
            {isRateLimited
              ? 'You have sent too many verification requests in a short period.'
              : 'An unexpected system or network error occurred during verification.'}
          </p>
          <p className="text-xs text-muted-foreground leading-relaxed">{message}</p>
        </div>

        {onRetry && (
          <div className="flex justify-end pt-1">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={onRetry}
              className="gap-1.5"
            >
              <RotateCcw className="h-4 w-4" />
              Try Again
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
