import { Navigate, Outlet } from 'react-router-dom';
import { useFamily } from '../hooks/useFamily';
import { canAccessExpenses } from '../utils/roles';

/**
 * Sends children to the dashboard instead of Family Expenses. Cosmetic: the API returns 403 to
 * children on every expense endpoint.
 */
export function RequireExpensesAccess() {
  const { role } = useFamily();

  return canAccessExpenses(role) ? <Outlet /> : <Navigate to="/dashboard" replace />;
}
