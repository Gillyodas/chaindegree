import React from 'react';
import { AlertOctagon, Ban } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Badge } from '@/shared/components/ui/badge';
import { formatDate } from '@/shared/lib/date';
import type { VerifyDegreeSuccessResponse } from '../verification.types';

export interface RevokedResultProps {
  data: VerifyDegreeSuccessResponse;
}

export function RevokedResult({ data }: RevokedResultProps) {
  return (
    <Card
      className="border-2 border-destructive/80 bg-destructive/5 dark:bg-destructive/10 shadow-lg animate-in fade-in zoom-in-95 duration-300"
      role="alert"
      aria-label="Degree has been revoked"
    >
      <CardHeader className="pb-3 border-b border-destructive/20">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-destructive/10 rounded-full text-destructive">
              <Ban className="h-6 w-6" />
            </div>
            <div>
              <CardTitle className="text-xl font-bold text-destructive flex items-center gap-2">
                Degree Revoked
              </CardTitle>
              <p className="text-xs text-destructive/80 mt-0.5">
                This academic credential has been officially revoked and is no longer valid.
              </p>
            </div>
          </div>
          <Badge variant="destructive" className="font-semibold text-xs px-3 py-1 uppercase">
            Revoked
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="pt-5 space-y-5">
        <div className="p-3.5 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-sm font-medium flex items-center gap-2.5">
          <AlertOctagon className="h-5 w-5 shrink-0" />
          <span>
            Notice: This degree record was found in the official registry, but its status has been revoked by the issuing institution.
          </span>
        </div>

        {/* Academic Details Grid (Muted) */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-muted-foreground">
          <div className="space-y-1">
            <span className="text-xs font-medium uppercase tracking-wider">Degree Code</span>
            <p className="font-mono font-bold text-base text-foreground/80">{data.degreeCode}</p>
          </div>

          <div className="space-y-1">
            <span className="text-xs font-medium uppercase tracking-wider">Institution</span>
            <p className="font-semibold text-base text-foreground/80">
              {data.institutionName || 'Unknown Institution'}
            </p>
          </div>

          {data.studentFullName && (
            <div className="space-y-1">
              <span className="text-xs font-medium uppercase tracking-wider">Student Name</span>
              <p className="font-semibold text-base text-foreground/80">{data.studentFullName}</p>
            </div>
          )}

          <div className="space-y-1">
            <span className="text-xs font-medium uppercase tracking-wider">Major</span>
            <p className="font-medium text-base text-foreground/80">{data.major || 'N/A'}</p>
          </div>

          <div className="space-y-1">
            <span className="text-xs font-medium uppercase tracking-wider">Classification</span>
            <p className="font-medium text-base text-foreground/80">{data.classification || 'N/A'}</p>
          </div>

          {data.issuedAt && (
            <div className="space-y-1">
              <span className="text-xs font-medium uppercase tracking-wider">Issued Date</span>
              <p className="font-medium text-base text-foreground/80">{formatDate(data.issuedAt)}</p>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
