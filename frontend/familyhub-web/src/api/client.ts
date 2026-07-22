import axios from 'axios';
import { API_BASE_URL } from '../utils/constants';
import { clearToken, getToken } from '../utils/token';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

// Attach the JWT to every request.
apiClient.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// On an expired/invalid token, clear it and send the user back to login.
// Auth endpoints are excluded so a failed login/register shows its own error.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status: number | undefined = error?.response?.status;
    const url: string = error?.config?.url ?? '';
    const isAuthEndpoint = url.includes('/auth/login') || url.includes('/auth/register');

    if (status === 401 && !isAuthEndpoint) {
      clearToken();
      if (window.location.pathname !== '/login') {
        window.location.assign('/login');
      }
    }

    return Promise.reject(error);
  },
);

export default apiClient;
