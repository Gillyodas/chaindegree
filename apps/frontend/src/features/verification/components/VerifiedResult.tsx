import React, { useState } from 'react';
import { CheckCircle2, Copy, Check, ExternalLink, ShieldCheck } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { formatDate } from '@/shared/lib/date';
import type { VerifyDegreeSuccessResponse } from '../verification.types';

export interface VerifiedResultProps {
  data: VerifyDegreeSuccessResponse;
}

export function VerifiedResult({ data }: VerifiedResultProps) {
  const [copied, setCopied] = useState(false);

  const handleCopyTx = () => {
    if (data.blockchain?.txHash) {
      navigator.clipboard.writeText(data.blockchain.txHash);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const truncateHash = (hash: string) => {
    if (!hash || hash.length <= 16) return hash;
    return `${hash.slice(0, 10)}...${hash.slice(-8)}`;
  };

  const formattedSource = data.verificationSource
    ? data.verificationSource.replace(/_/g, ' ')
    : 'Blockchain Merkle Root';

  return (
    <Card
      className="border-2 border-emerald-500/80 bg-emerald-50/30 dark:bg-emerald-950/10 shadow-lg animate-in fade-in zoom-in-95 duration-300"
      role="status"
      aria-label="Degree verified successfully"
    >
      <CardHeader className="pb-3 border-b border-emerald-200/50 dark:border-emerald-900/50">
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-emerald-500/10 rounded-full text-emerald-600 dark:text-emerald-400">
              <CheckCircle2 className="h-6 w-6" />
            </div>
            <div>
              <CardTitle className="text-xl font-bold text-emerald-900 dark:text-emerald-100 flex items-center gap-2">
                Degree Verified & Valid
              </CardTitle>
              <p className="text-xs text-emerald-700/80 dark:text-emerald-300/80 mt-0.5">
                Official authenticity confirmed against the cryptographic ledger
              </p>
            </div>
          </div>
          <Badge className="bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-xs px-3 py-1">
            Confirmed (v{data.version})
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="pt-5 space-y-6">
        {/* Academic Details Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="space-y-1">
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Degree Code
            </span>
            <p className="font-mono font-bold text-base text-foreground">{data.degreeCode}</p>
          </div>

          <div className="space-y-1">
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Institution
            </span>
            <p className="font-semibold text-base text-foreground">
              {data.institutionName || 'Unknown Institution'}
            </p>
          </div>

          {data.studentFullName && (
            <div className="space-y-1">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Student Name
              </span>
              <p className="font-semibold text-base text-foreground">{data.studentFullName}</p>
            </div>
          )}

          <div className="space-y-1">
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Major / Specialization
            </span>
            <p className="font-medium text-base text-foreground">{data.major || 'N/A'}</p>
          </div>

          <div className="space-y-1">
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Classification
            </span>
            <p className="font-medium text-base text-foreground">{data.classification || 'N/A'}</p>
          </div>

          {data.issuedAt && (
            <div className="space-y-1">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                Issued Date
              </span>
              <p className="font-medium text-base text-foreground">{formatDate(data.issuedAt)}</p>
            </div>
          )}
        </div>

        {/* Blockchain Proof Section */}
        {data.blockchain && (
          <div className="p-4 rounded-lg bg-card border border-border/80 space-y-3">
            <div className="flex items-center gap-2 text-xs font-semibold text-foreground uppercase tracking-wider">
              <ShieldCheck className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
              <span>Blockchain Verification Proof</span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div>
                <span className="text-muted-foreground">Source:</span>{' '}
                <span className="font-medium text-foreground">{formattedSource}</span>
              </div>
              {data.blockchain.blockNumber != null && (
                <div>
                  <span className="text-muted-foreground">Block Number:</span>{' '}
                  <span className="font-mono font-medium text-foreground">
                    #{data.blockchain.blockNumber}
                  </span>
                </div>
              )}
            </div>

            {data.blockchain.txHash && (
              <div className="pt-1 flex items-center justify-between gap-2 p-2 bg-muted/60 rounded font-mono text-xs">
                <div className="flex items-center gap-1.5 min-w-0">
                  <span className="text-muted-foreground shrink-0">Tx:</span>
                  <span
                    className="truncate text-foreground select-all"
                    title={data.blockchain.txHash}
                  >
                    {truncateHash(data.blockchain.txHash)}
                  </span>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={handleCopyTx}
                  className="h-7 px-2 text-xs text-muted-foreground hover:text-foreground shrink-0"
                  aria-label="Copy Transaction Hash"
                >
                  {copied ? (
                    <Check className="h-3.5 w-3.5 text-emerald-600" />
                  ) : (
                    <Copy className="h-3.5 w-3.5" />
                  )}
                  <span className="ml-1 sr-only">{copied ? 'Copied' : 'Copy'}</span>
                </Button>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
