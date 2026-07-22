import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { LanguageSwitcher } from './LanguageSwitcher';

export function Header() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();

  return (
    <header className="sticky top-0 z-10 flex h-16 items-center justify-between border-b border-gray-200 bg-white/80 px-4 backdrop-blur md:px-6">
      <div className="flex items-center gap-2 md:hidden">
        <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-sm font-bold text-white">
          FH
        </span>
        <span className="font-semibold text-gray-900">{t('app.name')}</span>
      </div>

      <div className="hidden text-sm text-gray-500 md:block">
        {user ? `${t('common.welcome')}, ${user.firstName}` : ''}
      </div>

      <div className="flex items-center gap-3">
        <LanguageSwitcher />
        <button
          type="button"
          onClick={logout}
          className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm font-medium text-gray-600 hover:bg-gray-50"
        >
          {t('common.logout')}
        </button>
      </div>
    </header>
  );
}
