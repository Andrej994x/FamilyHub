import { createContext, useCallback, useEffect, useState, type ReactNode } from 'react';
import type { LoginRequest, RegisterRequest, User } from '../types';
import { authService } from '../services/authService';
import { clearToken, getToken, setToken } from '../utils/token';

export interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (payload: LoginRequest) => Promise<void>;
  register: (payload: RegisterRequest) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Restore the session on first load if a token is present.
  useEffect(() => {
    const token = getToken();
    if (!token) {
      setIsLoading(false);
      return;
    }

    authService
      .me()
      .then(setUser)
      .catch(() => clearToken())
      .finally(() => setIsLoading(false));
  }, []);

  const login = useCallback(async (payload: LoginRequest) => {
    const response = await authService.login(payload);
    setToken(response.token);
    setUser(response.user);
  }, []);

  const register = useCallback(
    async (payload: RegisterRequest) => {
      await authService.register(payload);
      await login({ email: payload.email, password: payload.password });
    },
    [login],
  );

  const logout = useCallback(() => {
    clearToken();
    setUser(null);
  }, []);

  const value: AuthContextValue = {
    user,
    isAuthenticated: user !== null,
    isLoading,
    login,
    register,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
