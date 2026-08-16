import { ShieldAlert, AlertTriangle } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Badge } from '@/shared/components/ui/badge';
import type { VerificationErrorCode } from '../verification.types';

export interface TamperedWarningProps {
  errorCode: VerificationErrorCode | string;
  message?: string;
}

export function TamperedWarning({ errorCode, message }: TamperedWarningProps) {
  const isHashMismatch = errorCode === 'CRYPTO_HASH_MISMATCH';

  return (
    <>
      <style>{`
        @keyframes pulse-tampered-border {
          0%, 100% {
            border-color: rgb(249, 115, 22); /* orange-500 */
            box-shadow: 0 0 15px rgba(249, 115, 22, 0.3);
          }
          50% {
            border-color: rgb(253, 186, 116); /* orange-300 */
            box-shadow: 0 0 5px rgba(249, 115, 22, 0.1);
          }
        }
        .animate-tampered-pulse {
          animation: pulse-tampered-border 2s ease-in-out infinite;
        }
      `}</style>

      <Card
        className="border-2 border-orange-500 bg-orange-50/50 dark:bg-orange-950/20 shadow-xl animate-tampered-pulse animate-in fade-in zoom-in-95 duration-300"
        role="alert"
        aria-live="assertive"
        aria-label="Critical Warning: Data integrity compromised"
      >
        <CardHeader className="pb-3 border-b border-orange-200 dark:border-orange-900/50">
          <div className="flex items-center justify-between flex-wrap gap-2">
            <div className="flex items-center gap-2.5">
              <div className="p-2 bg-orange-500/10 rounded-full text-orange-600 dark:text-orange-400">
                <ShieldAlert className="h-6 w-6 animate-bounce" />
              </div>
              <div>
                <CardTitle className="text-xl font-bold text-orange-900 dark:text-orange-200 flex items-center gap-2">
                  Integrity Verification Failed
                </CardTitle>
                <p className="text-xs text-orange-700/90 dark:text-orange-300/90 mt-0.5">
                  High Severity Security Warning
                </p>
              </div>
            </div>
            <Badge className="bg-orange-600 hover:bg-orange-700 text-white font-semibold text-xs px-3 py-1">
              Tampered / Mismatch
            </Badge>
          </div>
        </CardHeader>

        <CardContent className="pt-5 space-y-4 text-orange-950 dark:text-orange-100">
          <div className="p-4 rounded-lg bg-orange-100/80 dark:bg-orange-900/40 border border-orange-300 dark:border-orange-800 space-y-2">
            <div className="flex items-start gap-2.5">
              <AlertTriangle className="h-5 w-5 text-orange-600 dark:text-orange-400 shrink-0 mt-0.5" />
              <div className="text-sm font-semibold">
                {isHashMismatch
                  ? 'CRITICAL WARNING: Cryptographic Hash Mismatch'
                  : 'CRITICAL WARNING: Blockchain Ledger Validation Failed'}
              </div>
            </div>
            <p className="text-xs leading-relaxed text-orange-900/90 dark:text-orange-200/90 pl-7.5">
              {isHashMismatch
                ? 'Data integrity compromised. The calculated cryptographic hash does not match official records. The underlying data may have been altered or tampered with.'
                : 'Blockchain verification failed. The degree record could not be validated against the blockchain network. Data integrity cannot be confirmed.'}
            </p>
          </div>

          <div className="text-xs text-muted-foreground bg-background/60 p-3 rounded border border-border/60">
            <span className="font-semibold text-foreground">Diagnostic Code:</span>{' '}
            <code className="font-mono text-orange-600 dark:text-orange-400 font-bold">{errorCode}</code>
            {message && <p className="mt-1 text-muted-foreground">{message}</p>}
          </div>
        </CardContent>
      </Card>
    </>
  );
}
