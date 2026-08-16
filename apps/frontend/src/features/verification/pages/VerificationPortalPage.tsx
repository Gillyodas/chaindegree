import React, { useState, useCallback } from 'react';
import { ShieldCheck, Shield, Lock, Layers, Loader2 } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Badge } from '@/shared/components/ui/badge';
import { VerificationForm } from '../components/VerificationForm';
import { VerificationResult } from '../components/VerificationResult';
import { useVerifyDegree } from '../hooks/useVerifyDegree';
import { useDegreeVersions } from '../hooks/useDegreeVersions';

export function VerificationPortalPage() {
  const [degreeCode, setDegreeCode] = useState('');
  const [selectedVersion, setSelectedVersion] = useState<number | null>(null);

  const {
    verify,
    result,
    isPending: isVerifying,
    clearResult,
    inputError,
  } = useVerifyDegree();

  const {
    versions,
    isLoading: versionsLoading,
    degreeNotFound,
  } = useDegreeVersions(degreeCode);

  const handleDegreeCodeChange = useCallback(() => {
    clearResult();
  }, [clearResult]);

  const handleVersionChange = useCallback(() => {
    clearResult();
  }, [clearResult]);

  const handleSubmit = useCallback(
    ({ degreeCode: submittedCode, version }: { degreeCode: string; version?: number | null }) => {
      setSelectedVersion(version ?? null);
      verify({
        degreeCode: submittedCode,
        version: version ?? null,
      });
    },
    [verify],
  );

  const handleRetry = useCallback(() => {
    if (degreeCode.trim()) {
      verify({
        degreeCode: degreeCode.trim(),
        version: selectedVersion,
      });
    }
  }, [degreeCode, selectedVersion, verify]);

  return (
    <div className="min-h-full py-8 sm:py-12 px-4 sm:px-6 lg:px-8 max-w-4xl mx-auto space-y-8 animate-in fade-in duration-300">
      {/* Header Banner */}
      <div className="text-center space-y-3">
        <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-primary/10 text-primary text-xs font-semibold tracking-wide uppercase">
          <ShieldCheck className="h-4 w-4" />
          <span>Public Verification Service</span>
        </div>
        <h1 className="text-3xl sm:text-4xl font-extrabold text-foreground tracking-tight">
          Verify Degree Authenticity
        </h1>
        <p className="text-muted-foreground max-w-2xl mx-auto text-sm sm:text-base leading-relaxed">
          Verify any digital degree issued on the ChainDegree network. Instant validation against our dual cryptographic ledger and immutable blockchain root.
        </p>
      </div>

      {/* Main Verification Card */}
      <Card className="shadow-xl border-border/80 bg-card">
        <CardHeader className="pb-4 border-b border-border/40">
          <CardTitle className="text-lg font-semibold flex items-center gap-2">
            <Shield className="h-5 w-5 text-primary" />
            Lookup Degree Record
          </CardTitle>
          <CardDescription>
            Enter the official Degree Identifier to query the cryptographic verification ledger.
          </CardDescription>
        </CardHeader>
        <CardContent className="pt-6">
          <VerificationForm
            onSubmit={handleSubmit}
            isSubmitting={isVerifying}
            inputError={inputError}
            onDegreeCodeChange={handleDegreeCodeChange}
            onVersionChange={handleVersionChange}
            degreeCode={degreeCode}
            onDegreeCodeInputChange={setDegreeCode}
            versions={versions}
            versionsLoading={versionsLoading}
            degreeNotFound={degreeNotFound}
          />
        </CardContent>
      </Card>

      {/* Verification In-Progress Indicator */}
      {isVerifying && (
        <Card className="border border-primary/30 bg-primary/5 shadow-md p-6 text-center animate-in fade-in zoom-in-95 duration-200">
          <div className="flex flex-col items-center justify-center space-y-3">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <div className="space-y-1">
              <h3 className="text-base font-semibold text-foreground">
                Verifying Cryptographic Ledger...
              </h3>
              <p className="text-xs text-muted-foreground">
                Comparing local record hash against the blockchain immutable Merkle root.
              </p>
            </div>
          </div>
        </Card>
      )}

      {/* Result Display Section */}
      {!isVerifying && result && (
        <div className="space-y-4">
          <VerificationResult result={result} onRetry={handleRetry} />
        </div>
      )}

      {/* Educational Trust Section */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-4 text-xs text-muted-foreground border-t border-border/40">
        <div className="p-4 rounded-lg bg-muted/40 border border-border/40 space-y-1.5">
          <div className="flex items-center gap-1.5 font-semibold text-foreground">
            <Lock className="h-4 w-4 text-primary" />
            <span>Cryptographic Hashing</span>
          </div>
          <p>
            Every degree is salted and canonicalized to guarantee zero tampering of student details.
          </p>
        </div>

        <div className="p-4 rounded-lg bg-muted/40 border border-border/40 space-y-1.5">
          <div className="flex items-center gap-1.5 font-semibold text-foreground">
            <Layers className="h-4 w-4 text-primary" />
            <span>Merkle Proof Verification</span>
          </div>
          <p>
            Batch roots are anchored immutably to the Hyperledger Besu enterprise blockchain ledger.
          </p>
        </div>

        <div className="p-4 rounded-lg bg-muted/40 border border-border/40 space-y-1.5">
          <div className="flex items-center gap-1.5 font-semibold text-foreground">
            <ShieldCheck className="h-4 w-4 text-primary" />
            <span>Instant Public Trust</span>
          </div>
          <p>
            Employers and academic institutions can verify credentials globally without login.
          </p>
        </div>
      </div>
    </div>
  );
}

export default VerificationPortalPage;
