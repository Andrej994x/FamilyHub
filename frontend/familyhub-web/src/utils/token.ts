import { TOKEN_STORAGE_KEY } from './constants';

export const getToken = (): string | null => localStorage.getItem(TOKEN_STORAGE_KEY);

export const setToken = (token: string): void => localStorage.setItem(TOKEN_STORAGE_KEY, token);

export const clearToken = (): void => localStorage.removeItem(TOKEN_STORAGE_KEY);
