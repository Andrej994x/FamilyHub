import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { AuthLayout } from '../layouts/AuthLayout';
import { FullPageLoader } from '../components/FullPageLoader';

/**
 * Routes for unauthenticated users (login/register). Authenticated users are
 * redirected to the dashboard. Wraps children in the centered auth layout.
 */
export function PublicRoute() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <FullPageLoader />;
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <AuthLayout>
      <Outlet />
    </AuthLayout>
  );
}
