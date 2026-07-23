import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { AuthLayout } from '../layouts/AuthLayout';
import { FullPageLoader } from '../components/FullPageLoader';
import { postAuthRedirectPath } from '../utils/pendingInvitation';

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
    // Continue a pending invitation if one is waiting, otherwise go to the dashboard.
    return <Navigate to={postAuthRedirectPath()} replace />;
  }

  return (
    <AuthLayout>
      <Outlet />
    </AuthLayout>
  );
}
