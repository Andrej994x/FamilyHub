import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { LanguageSwitcher } from '../components/LanguageSwitcher';

export function AuthLayout({ children }: { children: ReactNode }) {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen flex-col bg-gradient-to-br from-brand-50 via-gray-50 to-gray-100">
      <div className="flex items-center justify-between p-4 md:p-6">
        <div className="flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-brand-600 text-sm font-bold text-white">
            FH
          </span>
          <span className="text-lg font-semibold text-gray-900">{t('app.name')}</span>
        </div>
        <LanguageSwitcher />
      </div>

      <div className="flex flex-1 items-center justify-center p-4">
        <div className="w-full max-w-md">
          <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm sm:p-8">
            {children}
          </div>
          <p className="mt-6 text-center text-xs text-gray-400">{t('app.tagline')}</p>
        </div>
      </div>
    </div>
  );
}
