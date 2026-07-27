// Resolve the API base URL:
//  1. An explicit VITE_API_BASE_URL always wins (point the app at any API host).
//  2. On the Vite dev server (port 5173) talk to the API on port 5271 at the same hostname,
//     so the app can be opened from a phone on the LAN and still reach the API.
//  3. Otherwise the app is served by the API itself (single-origin PWA hosting / production),
//     so use a same-origin relative path. This keeps working across refreshes and deep links
//     and avoids any CORS from an installed PWA.
function resolveApiBaseUrl(): string {
  const override = import.meta.env.VITE_API_BASE_URL;
  if (override) {
    return override;
  }
  if (typeof window === 'undefined') {
    return 'http://localhost:5271/api';
  }
  if (window.location.port === '5173') {
    return `${window.location.protocol}//${window.location.hostname}:5271/api`;
  }
  return '/api';
}

export const API_BASE_URL = resolveApiBaseUrl();

export const TOKEN_STORAGE_KEY = 'familyhub_token';

export const LANGUAGE_STORAGE_KEY = 'familyhub_language';
