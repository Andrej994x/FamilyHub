import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { FullPageLoader } from '../components/FullPageLoader';

export function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <FullPageLoader />;
  }

  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}
