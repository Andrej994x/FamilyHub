import { Outlet } from 'react-router-dom';
import { FamilyProvider } from '../contexts/FamilyContext';

/** Provides the family context to every authenticated route. */
export function FamilyGate() {
  return (
    <FamilyProvider>
      <Outlet />
    </FamilyProvider>
  );
}
