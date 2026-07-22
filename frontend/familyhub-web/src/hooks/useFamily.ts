import { useContext } from 'react';
import { FamilyContext, type FamilyContextValue } from '../contexts/FamilyContext';

export function useFamily(): FamilyContextValue {
  const context = useContext(FamilyContext);
  if (context === undefined) {
    throw new Error('useFamily must be used within a FamilyProvider');
  }
  return context;
}
