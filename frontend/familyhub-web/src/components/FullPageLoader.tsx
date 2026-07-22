import { useTranslation } from 'react-i18next';

export function FullPageLoader() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50">
      <div className="flex flex-col items-center gap-3">
        <div className="h-10 w-10 animate-spin rounded-full border-4 border-gray-200 border-t-brand-600" />
        <p className="text-sm text-gray-500">{t('common.loading')}</p>
      </div>
    </div>
  );
}
