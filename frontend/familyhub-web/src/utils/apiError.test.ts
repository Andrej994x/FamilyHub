import { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios';
import { describe, expect, it } from 'vitest';
import { getApiErrorMessage } from './apiError';

function axiosErrorWith(data: unknown, status = 400): AxiosError {
  const config = { headers: new AxiosHeaders() } as InternalAxiosRequestConfig;
  return new AxiosError('Request failed', 'ERR_BAD_REQUEST', config, null, {
    data,
    status,
    statusText: '',
    headers: {},
    config,
  });
}

describe('getApiErrorMessage', () => {
  it('returns the first field error of an ASP.NET validation problem', () => {
    const error = axiosErrorWith({
      title: 'One or more validation errors occurred.',
      errors: { Title: ['Title is required.'], Priority: ['A valid priority is required.'] },
    });

    expect(getApiErrorMessage(error, 'fallback')).toBe('Title is required.');
  });

  it('returns the message of the API ErrorResponse', () => {
    const error = axiosErrorWith({ status: 403, message: 'You do not have access to this family.' }, 403);

    expect(getApiErrorMessage(error, 'fallback')).toBe('You do not have access to this family.');
  });

  it('falls back to the problem title when there are no field errors', () => {
    const error = axiosErrorWith({ title: 'Conflict', errors: { Name: [] } }, 409);

    expect(getApiErrorMessage(error, 'fallback')).toBe('Conflict');
  });

  it('uses the fallback for network errors and non-Axios errors', () => {
    const networkError = new AxiosError('Network Error', 'ERR_NETWORK');

    expect(getApiErrorMessage(networkError, 'Something went wrong.')).toBe('Something went wrong.');
    expect(getApiErrorMessage(new Error('boom'), 'Something went wrong.')).toBe('Something went wrong.');
    expect(getApiErrorMessage(undefined, 'Something went wrong.')).toBe('Something went wrong.');
  });
});
