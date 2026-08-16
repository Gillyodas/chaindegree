import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router';
import { DashboardLayout } from '@/app/layouts/DashboardLayout';
import { PublicLayout } from '@/app/layouts/PublicLayout';
import { ProtectedRoute } from '@/app/router/ProtectedRoute';
import { ErrorBoundary } from '@/shared/components/ErrorBoundary';
import { LoginPage } from '@/features/auth';

// Lazy loading page-level components ONLY
const DegreeListPage = lazy(() =>
  import('@/features/degree').then((m) => ({ default: m.DegreeListPage })),
);
const DegreeDetailPage = lazy(() =>
  import('@/features/degree').then((m) => ({ default: m.DegreeDetailPage })),
);
const IssueDegreeForm = lazy(() =>
  import('@/features/degree').then((m) => ({ default: m.IssueDegreeForm })),
);
const DegreeComingSoonPage = lazy(() =>
  import('@/features/degree').then((m) => ({ default: m.DegreeComingSoonPage })),
);
const VerificationPortalPage = lazy(() =>
  import('@/features/verification').then((m) => ({ default: m.VerificationPortalPage })),
);
const ReportComingSoonPage = lazy(() =>
  import('@/features/report').then((m) => ({ default: m.ReportComingSoonPage })),
);
const ReputationComingSoonPage = lazy(() =>
  import('@/features/reputation').then((m) => ({ default: m.ReputationComingSoonPage })),
);
const RecruitmentComingSoonPage = lazy(() =>
  import('@/features/recruitment').then((m) => ({ default: m.RecruitmentComingSoonPage })),
);

function SuspenseFallback() {
  return (
    <div className="flex h-full min-h-[300px] items-center justify-center">
      <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
    </div>
  );
}

export function AppRouter() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <Suspense fallback={<SuspenseFallback />}>
          <Routes>
            {/* Public Auth Page */}
            <Route path="/login" element={<LoginPage />} />

            {/* Public Unauthenticated Portal Routes */}
            <Route element={<PublicLayout />}>
              <Route path="/verify" element={<VerificationPortalPage />} />
            </Route>

            {/* Protected Dashboard Routes */}
            <Route element={<ProtectedRoute />}>
              <Route element={<DashboardLayout />}>
                {/* Default Dashboard Home */}
                <Route
                  path="/"
                  element={<DegreeComingSoonPage title="ChainDegree Overview Dashboard" />}
                />

                {/* Degree Management Routes (Registrar Only) */}
                <Route element={<ProtectedRoute allowedRoles={['Registrar']} />}>
                  <Route path="/degrees" element={<DegreeListPage />} />
                  <Route path="/degrees/:id" element={<DegreeDetailPage />} />
                  <Route path="/degrees/issue" element={<IssueDegreeForm />} />
                </Route>

                {/* Student Degree Route (Student Only) */}
                <Route element={<ProtectedRoute allowedRoles={['Student']} />}>
                  <Route
                    path="/my-degrees"
                    element={<DegreeComingSoonPage title="My Degrees (Student View)" />}
                  />
                </Route>

                {/* Admin Report Review Route (Admin Only) */}
                <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
                  <Route path="/admin/reports" element={<ReportComingSoonPage />} />
                </Route>

                {/* Reputation Dashboard Route */}
                <Route path="/reputation" element={<ReputationComingSoonPage />} />

                {/* Recruitment Routes (Student & Recruiter) */}
                <Route path="/jobs" element={<RecruitmentComingSoonPage />} />
                <Route path="/applications" element={<RecruitmentComingSoonPage />} />
              </Route>
            </Route>

            {/* Fallback Catch-all Route */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Suspense>
      </BrowserRouter>
    </ErrorBoundary>
  );
}

export default AppRouter;
