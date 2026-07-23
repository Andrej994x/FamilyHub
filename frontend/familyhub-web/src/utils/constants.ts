// Default to the same host the app is served from, on the API port, so opening the app
// from a phone on the LAN (e.g. http://192.168.x.x:5173) talks to the API at the same IP.
// Override with VITE_API_BASE_URL when the API lives elsewhere.
const inferredApiBase =
  typeof window !== 'undefined'
    ? `${window.location.protocol}//${window.location.hostname}:5271/api`
    : 'http://localhost:5271/api';

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? inferredApiBase;

export const TOKEN_STORAGE_KEY = 'familyhub_token';

export const LANGUAGE_STORAGE_KEY = 'familyhub_language';
