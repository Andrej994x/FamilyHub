import { useTranslation } from 'react-i18next';

interface PlaceholderPageProps {
  titleKey: string;
  subtitleKey: string;
}

export function PlaceholderPage({ titleKey, subtitleKey }: PlaceholderPageProps) {
  const { t } = useTranslation();

  return (
    <div className="mx-auto max-w-4xl">
      <h1 className="text-2xl font-semibold text-gray-900">{t(titleKey)}</h1>
      <p className="mt-1 text-sm text-gray-500">{t(subtitleKey)}</p>

      <div className="mt-6 flex items-center justify-center rounded-2xl border-2 border-dashed border-gray-200 bg-white p-16 text-center text-sm text-gray-400">
        {t('common.comingSoon')}
      </div>
    </div>
  );
}
