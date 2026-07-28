import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { PushNotificationToggle } from '../components/PushNotificationToggle';

export default function SystemSettings() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  return (
    <div className="mx-auto max-w-2xl">
      <header className="mb-5">
        <h1 className="text-2xl font-semibold text-gray-900">{t('settings.title')}</h1>
        <p className="mt-1 text-sm text-gray-500">{t('settings.subtitle')}</p>
      </header>

      <div className="space-y-3">
        {/* Language */}
        <div className="flex items-center justify-between gap-4 rounded-xl border border-gray-200 bg-white p-4">
          <div className="min-w-0">
            <h2 className="text-sm font-semibold text-gray-900">{t('settings.language.title')}</h2>
            <p className="mt-0.5 text-xs text-gray-500">{t('settings.language.subtitle')}</p>
          </div>
          <LanguageSwitcher />
        </div>

        {/* Push notifications (self-contained card) */}
        <PushNotificationToggle />

        {/* Reminders — links to the detailed per-category screen */}
        <button
          type="button"
          onClick={() => navigate('/reminders')}
          className="flex w-full items-center justify-between gap-3 rounded-xl border border-gray-200 bg-white p-4 text-left hover:bg-gray-50"
        >
          <div className="min-w-0">
            <h2 className="text-sm font-semibold text-gray-900">{t('settings.reminders.title')}</h2>
            <p className="mt-0.5 text-xs text-gray-500">{t('settings.reminders.subtitle')}</p>
          </div>
          <svg className="h-4 w-4 shrink-0 text-gray-300" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
          </svg>
        </button>

        {/* Account */}
        <div className="flex items-center justify-between gap-4 rounded-xl border border-gray-200 bg-white p-4">
          <div className="min-w-0">
            <h2 className="text-sm font-semibold text-gray-900">{t('settings.account.title')}</h2>
            <p className="mt-0.5 truncate text-xs text-gray-500">
              {t('settings.account.subtitle', { name: user ? `${user.firstName} ${user.lastName}` : '' })}
            </p>
          </div>
          <button
            type="button"
            onClick={logout}
            className="shrink-0 rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {t('common.logout')}
          </button>
        </div>
      </div>
    </div>
  );
}
