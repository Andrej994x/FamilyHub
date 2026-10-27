import { AxiosError, type AxiosAdapter, type InternalAxiosRequestConfig } from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import apiClient from './client';
import { TOKEN_STORAGE_KEY } from '../utils/constants';

// Requests are answered at the Axios adapter boundary, so the real interceptors run.
const originalAdapter = apiClient.defaults.adapter;
let seenConfig: InternalAxiosRequestConfig | undefined;

function respondWith(status: number): AxiosAdapter {
  return async (config) => {
    seenConfig = config;
    const response = { data: {}, status, statusText: '', headers: {}, config };
    if (status >= 400) {
      throw new AxiosError(`Request failed with status code ${status}`, 'ERR_BAD_REQUEST', config, null, response);
    }
    return response;
  };
}

describe('apiClient', () => {
  const assign = vi.fn();

  beforeEach(() => {
    seenConfig = undefined;
    assign.mockReset();
    vi.stubGlobal('location', { pathname: '/tasks', assign });
  });

  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
  });

  it('attaches the stored JWT as a bearer token', async () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'jwt-123');
    apiClient.defaults.adapter = respondWith(200);

    await apiClient.get('/families/current');

    expect(seenConfig?.headers.Authorization).toBe('Bearer jwt-123');
  });

  it('sends no Authorization header when signed out', async () => {
    apiClient.defaults.adapter = respondWith(200);

    await apiClient.get('/families/current');

    expect(seenConfig?.headers.Authorization).toBeUndefined();
  });

  it('clears the token and redirects to /login on a 401', async () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'expired');
    apiClient.defaults.adapter = respondWith(401);

    await expect(apiClient.get('/families/current')).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBeNull();
    expect(assign).toHaveBeenCalledWith('/login');
  });

  it('does not redirect when already on the login page', async () => {
    vi.stubGlobal('location', { pathname: '/login', assign });
    apiClient.defaults.adapter = respondWith(401);

    await expect(apiClient.get('/auth/me')).rejects.toBeInstanceOf(AxiosError);

    expect(assign).not.toHaveBeenCalled();
  });

  it('lets a failed login show its own error instead of redirecting', async () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'existing');
    apiClient.defaults.adapter = respondWith(401);

    await expect(apiClient.post('/auth/login', {})).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBe('existing');
    expect(assign).not.toHaveBeenCalled();
  });

  it('keeps the token on other errors', async () => {
    localStorage.setItem(TOKEN_STORAGE_KEY, 'valid');
    apiClient.defaults.adapter = respondWith(403);

    await expect(apiClient.get('/families/x/tasks')).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem(TOKEN_STORAGE_KEY)).toBe('valid');
    expect(assign).not.toHaveBeenCalled();
  });
});
