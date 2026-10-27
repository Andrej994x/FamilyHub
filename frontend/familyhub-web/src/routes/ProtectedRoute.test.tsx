import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ProtectedRoute } from './ProtectedRoute';
import { AuthContext, type AuthContextValue } from '../contexts/AuthContext';
import en from '../locales/en/translation.json';

function renderAt(auth: Pick<AuthContextValue, 'isAuthenticated' | 'isLoading'>) {
  const value: AuthContextValue = {
    user: null,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    ...auth,
  };

  render(
    <AuthContext.Provider value={value}>
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route path="/login" element={<p>login page</p>} />
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<p>dashboard content</p>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

describe('ProtectedRoute', () => {
  it('shows the loader while the session is being restored', () => {
    renderAt({ isAuthenticated: false, isLoading: true });

    expect(screen.getByText(en.common.loading)).toBeInTheDocument();
    expect(screen.queryByText('dashboard content')).not.toBeInTheDocument();
  });

  it('redirects signed-out users to the login page', () => {
    renderAt({ isAuthenticated: false, isLoading: false });

    expect(screen.getByText('login page')).toBeInTheDocument();
    expect(screen.queryByText('dashboard content')).not.toBeInTheDocument();
  });

  it('renders the protected page for signed-in users', () => {
    renderAt({ isAuthenticated: true, isLoading: false });

    expect(screen.getByText('dashboard content')).toBeInTheDocument();
  });
});
