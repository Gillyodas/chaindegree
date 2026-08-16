import React from 'react';
import { SearchX, Info } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Badge } from '@/shared/components/ui/badge';

export interface NotFoundResultProps {
  errorCode: string;
  message?: string;
}

export function NotFoundResult({ errorCode, message }: NotFoundResultProps) {
  const isVersionError = errorCode === 'UNSUPPORTED_VERSION';

  return (
    <Card
      className="border border-border/80 bg-muted/20 shadow-md animate-in fade-in zoom-in-95 duration-300"
      role="status"
      aria-label="Degree record not found"
    >
      <CardHeader className="pb-3 border-b border-border/50">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-muted rounded-full text-muted-foreground">
              <SearchX className="h-6 w-6" />
            </div>
            <div>
              <CardTitle className="text-xl font-bold text-foreground">
                {isVersionError ? 'Degree Version Not Found' : 'Degree Record Not Found'}
              </CardTitle>
              <p className="text-xs text-muted-foreground mt-0.5">
                {isVersionError
                  ? 'The requested version does not exist'
                  : 'No matching record exists in the official registry'}
              </p>
            </div>
          </div>
          <Badge variant="outline" className="text-muted-foreground font-semibold text-xs px-3 py-1">
            Not Found
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="pt-5 space-y-4">
        <div className="p-4 rounded-lg bg-background border border-border flex items-start gap-3">
          <Info className="h-5 w-5 text-muted-foreground shrink-0 mt-0.5" />
          <div className="text-sm text-foreground space-y-1">
            <p className="font-medium">
              {isVersionError
                ? 'The specified version number was not found for this degree.'
                : 'No degree found with the provided degree code.'}
            </p>
            <p className="text-xs text-muted-foreground leading-relaxed">
              {isVersionError
                ? 'Try verifying without specifying a version number to view the latest confirmed degree record.'
                : 'Please double-check the degree code format (e.g., DEG-2026-000001) and try again.'}
            </p>
          </div>
        </div>

        {message && (
          <p className="text-xs text-muted-foreground text-center italic">
            Server details: {message}
          </p>
        )}
      </CardContent>
    </Card>
  );
}
