import { createContext, useCallback, useEffect, useState, type ReactNode } from 'react';
import axios from 'axios';
import type { FamilyResponse } from '../types';
import { familyService } from '../services/familyService';
import { useAuth } from '../hooks/useAuth';

export interface FamilyContextValue {
  family: FamilyResponse | null;
  role: number | null;
  isLoading: boolean;
  refresh: () => Promise<void>;
  setFamily: (family: FamilyResponse | null) => void;
}

export const FamilyContext = createContext<FamilyContextValue | undefined>(undefined);

export function FamilyProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [family, setFamily] = useState<FamilyResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    try {
      const current = await familyService.getCurrent();
      setFamily(current);
    } catch (error) {
      // A 404 means the user simply has no family yet; treat any failure as "no family".
      if (!axios.isAxiosError(error) || error.response?.status !== 404) {
        // Non-404 errors are swallowed here; the onboarding screen will be shown.
      }
      setFamily(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isAuthenticated) {
      void refresh();
    } else {
      setFamily(null);
      setIsLoading(false);
    }
  }, [isAuthenticated, refresh]);

  const value: FamilyContextValue = {
    family,
    role: family?.currentUserRole ?? null,
    isLoading,
    refresh,
    setFamily,
  };

  return <FamilyContext.Provider value={value}>{children}</FamilyContext.Provider>;
}
