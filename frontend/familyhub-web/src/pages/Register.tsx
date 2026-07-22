import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import type { RegisterRequest } from '../types';

export default function Register() {
  const { t } = useTranslation();
  const { register: registerUser } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterRequest>();

  const onSubmit = async (values: RegisterRequest) => {
    setServerError(null);
    try {
      await registerUser(values);
      navigate('/dashboard', { replace: true });
    } catch {
      setServerError(t('auth.errors.registerFailed'));
    }
  };

  const inputClass =
    'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

  return (
    <div>
      <h1 className="text-xl font-semibold text-gray-900">{t('auth.register.title')}</h1>
      <p className="mt-1 text-sm text-gray-500">{t('auth.register.subtitle')}</p>

      <form className="mt-6 space-y-4" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('auth.fields.firstName')}
            </label>
            <input
              type="text"
              autoComplete="given-name"
              className={inputClass}
              {...register('firstName', { required: t('auth.errors.required') })}
            />
            {errors.firstName && (
              <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p>
            )}
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('auth.fields.lastName')}
            </label>
            <input
              type="text"
              autoComplete="family-name"
              className={inputClass}
              {...register('lastName', { required: t('auth.errors.required') })}
            />
            {errors.lastName && (
              <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p>
            )}
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('auth.fields.email')}
          </label>
          <input
            type="email"
            autoComplete="email"
            className={inputClass}
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
          <input
            type="password"
            autoComplete="new-password"
            className={inputClass}
            {...register('password', {
              required: t('auth.errors.required'),
              minLength: { value: 8, message: t('auth.errors.passwordMin') },
            })}
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
          {isSubmitting ? t('auth.buttons.submitting') : t('auth.buttons.signUp')}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-gray-500">
        {t('auth.links.haveAccount')}{' '}
        <Link to="/login" className="font-medium text-brand-600 hover:text-brand-700">
          {t('auth.links.toLogin')}
        </Link>
      </p>
    </div>
  );
}
