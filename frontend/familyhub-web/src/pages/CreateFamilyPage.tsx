import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { useFamily } from '../hooks/useFamily';
import { familyService } from '../services/familyService';
import { invitationService } from '../services/invitationService';
import { getApiErrorMessage } from '../utils/apiError';
import { LanguageSwitcher } from '../components/LanguageSwitcher';

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

export default function CreateFamilyPage() {
  const { t } = useTranslation();
  const { logout } = useAuth();
  const { family, setFamily, refresh } = useFamily();
  const navigate = useNavigate();

  const [name, setName] = useState('');
  const [token, setToken] = useState('');
  const [creating, setCreating] = useState(false);
  const [joining, setJoining] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [joinError, setJoinError] = useState<string | null>(null);

  // Already has a family — nothing to onboard.
  if (family) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!name.trim()) {
      return;
    }
    setCreating(true);
    setCreateError(null);
    try {
      const created = await familyService.create({ name: name.trim() });
      setFamily(created);
      navigate('/dashboard', { replace: true });
    } catch (err) {
      setCreateError(getApiErrorMessage(err, t('onboarding.errors.createFailed')));
    } finally {
      setCreating(false);
    }
  };

  const handleJoin = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!token.trim()) {
      return;
    }
    setJoining(true);
    setJoinError(null);
    try {
      await invitationService.accept(token.trim());
      await refresh();
      navigate('/dashboard', { replace: true });
    } catch (err) {
      setJoinError(getApiErrorMessage(err, t('onboarding.errors.joinFailed')));
    } finally {
      setJoining(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-brand-50 via-gray-50 to-gray-100">
      <header className="flex items-center justify-between p-4 md:p-6">
        <div className="flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-sm font-bold text-white">
            FH
          </span>
          <span className="text-lg font-semibold text-gray-900">{t('app.name')}</span>
        </div>
        <div className="flex items-center gap-3">
          <LanguageSwitcher />
          <button
            type="button"
            onClick={logout}
            className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-medium text-gray-600 hover:bg-gray-50"
          >
            {t('common.logout')}
          </button>
        </div>
      </header>

      <main className="mx-auto max-w-3xl px-4 py-6 md:py-10">
        <h1 className="text-2xl font-semibold text-gray-900 md:text-3xl">{t('onboarding.title')}</h1>
        <p className="mt-2 text-sm text-gray-500">{t('onboarding.subtitle')}</p>

        <div className="mt-8 grid grid-cols-1 gap-5 md:grid-cols-2">
          {/* Create a family */}
          <form
            onSubmit={handleCreate}
            className="flex flex-col rounded-2xl border border-gray-200 bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold text-gray-900">{t('onboarding.create.title')}</h2>
            <p className="mt-1 text-sm text-gray-500">{t('onboarding.create.subtitle')}</p>

            <label className="mb-1 mt-5 block text-sm font-medium text-gray-700">
              {t('onboarding.create.nameLabel')}
            </label>
            <input
              type="text"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder={t('onboarding.create.namePlaceholder')}
              className={inputClass}
            />

            {createError && (
              <div className="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
                {createError}
              </div>
            )}

            <button
              type="submit"
              disabled={creating}
              className="mt-4 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
            >
              {creating ? t('common.saving') : t('onboarding.create.submit')}
            </button>
          </form>

          {/* Join with a token */}
          <form
            onSubmit={handleJoin}
            className="flex flex-col rounded-2xl border border-gray-200 bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold text-gray-900">{t('onboarding.join.title')}</h2>
            <p className="mt-1 text-sm text-gray-500">{t('onboarding.join.subtitle')}</p>

            <label className="mb-1 mt-5 block text-sm font-medium text-gray-700">
              {t('onboarding.join.tokenLabel')}
            </label>
            <input
              type="text"
              value={token}
              onChange={(event) => setToken(event.target.value)}
              placeholder={t('onboarding.join.tokenPlaceholder')}
              className={`${inputClass} font-mono text-xs`}
            />

            {joinError && (
              <div className="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
                {joinError}
              </div>
            )}

            <button
              type="submit"
              disabled={joining}
              className="mt-4 rounded-lg border border-brand-600 px-4 py-2.5 text-sm font-semibold text-brand-700 hover:bg-brand-50 disabled:opacity-60"
            >
              {joining ? t('common.saving') : t('onboarding.join.submit')}
            </button>
          </form>
        </div>
      </main>
    </div>
  );
}
