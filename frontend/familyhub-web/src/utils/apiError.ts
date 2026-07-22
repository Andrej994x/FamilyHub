import axios from 'axios';

interface ApiErrorBody {
  title?: string;
  message?: string;
  errors?: Record<string, string[]>;
}

/**
 * Extracts a human-readable message from an API error.
 * Handles ASP.NET ProblemDetails validation payloads ({ errors }), the app's
 * ErrorResponse ({ message }), and falls back to the provided default.
 */
export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiErrorBody | undefined;

    if (data) {
      if (data.errors) {
        const firstField = Object.values(data.errors)[0];
        if (firstField && firstField.length > 0) {
          return firstField[0];
        }
      }
      if (data.message) {
        return data.message;
      }
      if (data.title) {
        return data.title;
      }
    }
  }

  return fallback;
}
