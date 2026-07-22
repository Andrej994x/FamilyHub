import { Navigate, Outlet } from 'react-router-dom';
import { useFamily } from '../hooks/useFamily';
import { FullPageLoader } from '../components/FullPageLoader';

/** Redirects users without a family to the onboarding screen. */
export function RequireFamily() {
  const { family, isLoading } = useFamily();

  if (isLoading) {
    return <FullPageLoader />;
  }

  return family ? <Outlet /> : <Navigate to="/onboarding" replace />;
}
