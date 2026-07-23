import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { PasswordInput } from '../components/PasswordInput';
import { getApiErrorMessage } from '../utils/apiError';
import { postAuthRedirectPath } from '../utils/pendingInvitation';
import type { LoginRequest } from '../types';

export default function Login() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginRequest>();

  const onSubmit = async (values: LoginRequest) => {
    setServerError(null);
    try {
      await login(values);
      navigate(postAuthRedirectPath(), { replace: true });
    } catch (error) {
      setServerError(getApiErrorMessage(error, t('auth.errors.invalidCredentials')));
    }
  };

  return (
    <div>
      <h1 className="text-xl font-semibold text-gray-900">{t('auth.login.title')}</h1>
      <p className="mt-1 text-sm text-gray-500">{t('auth.login.subtitle')}</p>

      <form className="mt-6 space-y-4" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('auth.fields.email')}
          </label>
          <input
            type="email"
            autoComplete="email"
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
            {...register('email', {
              required: t('auth.errors.required'),
              pattern: { value: /^\S+@\S+\.\S+$/, message: t('auth.errors.emailInvalid') },
            })}
          />
          {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('auth.fields.password')}
          </label>
          <PasswordInput
            autoComplete="current-password"
            {...register('password', { required: t('auth.errors.required') })}
          />
          {errors.password && (
            <p className="mt-1 text-xs text-red-600">{errors.password.message}</p>
          )}
        </div>

        {serverError && (
          <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{serverError}</div>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:opacity-60"
        >
          {isSubmitting ? t('auth.buttons.submitting') : t('auth.buttons.signIn')}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-gray-500">
        {t('auth.links.noAccount')}{' '}
        <Link to="/register" className="font-medium text-brand-600 hover:text-brand-700">
          {t('auth.links.toRegister')}
        </Link>
      </p>
    </div>
  );
}
