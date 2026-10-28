import type { ReactNode } from 'react';
import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import { AuthContext, type AuthContextValue } from '../contexts/AuthContext';
import { FamilyContext, type FamilyContextValue } from '../contexts/FamilyContext';
import type { FamilyResponse, User } from '../types';

export const testFamily: FamilyResponse = {
  id: 'family-1',
  name: 'Test family',
  createdByUserId: 'owner-1',
  createdAt: '2026-01-01T00:00:00Z',
  currentUserRole: 0,
  memberCount: 3,
};

export function testUser(id: string): User {
  return { id, firstName: 'Test', lastName: 'User', email: `${id}@test.familyhub`, createdAt: '2026-01-01T00:00:00Z' };
}

interface Options {
  role: number;
  userId?: string;
  initialEntries?: string[];
}

/** Renders `ui` signed in as `userId` with the given family role, inside a MemoryRouter. */
export function renderWithFamily(ui: ReactNode, { role, userId = 'user-1', initialEntries = ['/'] }: Options) {
  const auth: AuthContextValue = {
    user: testUser(userId),
    isAuthenticated: true,
    isLoading: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  };
  const family: FamilyContextValue = {
    family: { ...testFamily, currentUserRole: role },
    role,
    isLoading: false,
    refresh: vi.fn(),
    setFamily: vi.fn(),
  };

  return render(
    <AuthContext.Provider value={auth}>
      <FamilyContext.Provider value={family}>
        <MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>
      </FamilyContext.Provider>
    </AuthContext.Provider>,
  );
}
