import type { ReactNode } from 'react';
import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from './AuthContext';
import { useAuth } from '../hooks/useAuth';
import { authService } from '../services/authService';
import { TOKEN_STORAGE_KEY } from '../utils/constants';
import type { User } from '../types';

// The HTTP boundary for auth: the service object the provider calls.
vi.mock('../services/authService', () => ({
  authService: { login: vi.fn(), register: vi.fn(), me: vi.fn() },
}));

const user: User = {
  id: 'user-1',
  firstName: 'Ana',
  lastName: 'Petrova',
  email: 'ana@example.com',
  createdAt: '2026-01-01T00:00:00Z',
};

const wrapper = ({ children }: { children: ReactNode }) => <AuthProvider>{children}</AuthProvider>;

describe('AuthProvider and useAuth', () => {
  beforeEach(() => {
    vi.mocked(authService.login).mockReset();
    vi.mocked(authService.register).mockReset();
    vi.mocked(authService.me).mockReset();
  });

  it('throws when useAuth is used outside the provider', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    expect(() => renderHook(() => useAuth())).toThrow('useAuth must be used within an AuthProvider');
  });

  it('is signed out without a stored token and does not call the API', async () => {
    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.isAuthenticated).toBe(false);
    expect(authService.me).not.toHaveBeenCalled();
  });

  it('restores the session from a stored token', async () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'stored-jwt');
    vi.mocked(authService.me).mockResolvedValue(user);

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isLoading).toBe(true);
    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.user).toEqual(user);
    expect(result.current.isAuthenticated).toBe(true);
  });

  it('drops an invalid stored token', async () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'stale-jwt');
    vi.mocked(authService.me).mockRejectedValue(new Error('401'));

    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.isAuthenticated).toBe(false);
    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBeNull();
  });

  it('stores the token on login and clears it on logout', async () => {
    vi.mocked(authService.login).mockResolvedValue({ token: 'new-jwt', expiresAt: '2026-01-01T01:00:00Z', user });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(() => result.current.login({ email: user.email, password: 'Secret123' }));

    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBe('new-jwt');
    expect(result.current.user).toEqual(user);

    act(() => result.current.logout());

    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBeNull();
    expect(result.current.isAuthenticated).toBe(false);
  });

  it('signs in automatically after registering', async () => {
    vi.mocked(authService.register).mockResolvedValue(user);
    vi.mocked(authService.login).mockResolvedValue({ token: 'jwt', expiresAt: '2026-01-01T01:00:00Z', user });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.isLoading).toBe(false));

    await act(() =>
      result.current.register({ firstName: 'Ana', lastName: 'Petrova', email: user.email, password: 'Secret123' }),
    );

    expect(authService.login).toHaveBeenCalledWith({ email: user.email, password: 'Secret123' });
    expect(result.current.isAuthenticated).toBe(true);
  });
});
