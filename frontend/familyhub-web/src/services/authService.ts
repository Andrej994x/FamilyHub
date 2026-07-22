import apiClient from '../api/client';
import type { AuthResponse, LoginRequest, RegisterRequest, User } from '../types';

export const authService = {
  async login(payload: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/login', payload);
    return data;
  },

  async register(payload: RegisterRequest): Promise<User> {
    const { data } = await apiClient.post<User>('/auth/register', payload);
    return data;
  },

  async me(): Promise<User> {
    const { data } = await apiClient.get<User>('/auth/me');
    return data;
  },
};
