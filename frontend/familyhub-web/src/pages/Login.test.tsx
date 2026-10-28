import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import Login from './Login';
import { AuthContext, type AuthContextValue } from '../contexts/AuthContext';
import i18n from '../locales/i18n';
import en from '../locales/en/translation.json';
import mk from '../locales/mk/translation.json';
import { setPendingInvitationToken } from '../utils/pendingInvitation';

function Destination() {
  const location = useLocation();
  return <p data-testid="destination">{`${location.pathname}${location.search}`}</p>;
}

function renderLogin(login: AuthContextValue['login']) {
  const auth: AuthContextValue = {
    user: null,
    isAuthenticated: false,
    isLoading: false,
    login,
    register: vi.fn(),
    logout: vi.fn(),
  };

  render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="*" element={<Destination />} />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  );

  return {
    // The labels are not linked to their inputs, so query by role and autocomplete.
    email: screen.getByRole('textbox'),
    password: document.querySelector<HTMLInputElement>('input[autocomplete="current-password"]')!,
    // In whichever language is active.
    submit: screen.getByRole('button', { name: i18n.t('auth.buttons.signIn') }),
  };
}

function apiError(status: number, data: unknown): AxiosError {
  const config = { headers: new AxiosHeaders() } as InternalAxiosRequestConfig;
  return new AxiosError('failed', 'ERR_BAD_REQUEST', config, null, { status, data, statusText: '', headers: {}, config });
}

describe('Login page', () => {
  it('shows required-field errors and does not call login', async () => {
    const login = vi.fn();
    const { submit } = renderLogin(login);

    await userEvent.click(submit);

    expect(await screen.findAllByText(en.auth.errors.required)).toHaveLength(2);
    expect(login).not.toHaveBeenCalled();
  });

  it('rejects a malformed email address', async () => {
    const login = vi.fn();
    const { email, password, submit } = renderLogin(login);

    await userEvent.type(email, 'not-an-email');
    await userEvent.type(password, 'Secret123');
    await userEvent.click(submit);

    expect(await screen.findByText(en.auth.errors.emailInvalid)).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });

  it('shows a submitting state, then navigates to the dashboard on success', async () => {
    let finish: () => void = () => {};
    const login = vi.fn(() => new Promise<void>((resolve) => { finish = resolve; }));
    const { email, password, submit } = renderLogin(login);

    await userEvent.type(email, 'ana@example.com');
    await userEvent.type(password, 'Secret123');
    await userEvent.click(submit);

    const pending = await screen.findByRole('button', { name: en.auth.buttons.submitting });
    expect(pending).toBeDisabled();
    expect(login).toHaveBeenCalledWith({ email: 'ana@example.com', password: 'Secret123' });

    finish();

    expect(await screen.findByTestId('destination')).toHaveTextContent('/dashboard');
  });

  it('returns to a pending invitation after signing in', async () => {
    setPendingInvitationToken('invite-token');
    const { email, password, submit } = renderLogin(vi.fn().mockResolvedValue(undefined));

    await userEvent.type(email, 'ana@example.com');
    await userEvent.type(password, 'Secret123');
    await userEvent.click(submit);

    expect(await screen.findByTestId('destination')).toHaveTextContent('/invitations/accept?token=invite-token');
  });

  it('shows the server error message when sign-in fails', async () => {
    const login = vi.fn().mockRejectedValue(apiError(401, { status: 401, message: 'Invalid email or password.' }));
    const { email, password, submit } = renderLogin(login);

    await userEvent.type(email, 'ana@example.com');
    await userEvent.type(password, 'WrongPass1');
    await userEvent.click(submit);

    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: en.auth.buttons.signIn })).toBeEnabled();
  });

  it('falls back to the translated message when the server gives no details', async () => {
    const { email, password, submit } = renderLogin(vi.fn().mockRejectedValue(new Error('Network down')));

    await userEvent.type(email, 'ana@example.com');
    await userEvent.type(password, 'Secret123');
    await userEvent.click(submit);

    expect(await screen.findByText(en.auth.errors.invalidCredentials)).toBeInTheDocument();
  });

  it('renders in Macedonian, including validation messages', async () => {
    await i18n.changeLanguage('mk');
    const { submit } = renderLogin(vi.fn());

    expect(screen.getByRole('heading', { name: mk.auth.login.title })).toBeInTheDocument();
    expect(submit).toHaveTextContent(mk.auth.buttons.signIn);
    await userEvent.click(submit);

    await waitFor(() => expect(screen.getAllByText(mk.auth.errors.required)).toHaveLength(2));
    expect(screen.queryByText(en.auth.errors.required)).not.toBeInTheDocument();
  });
});
