import { useTranslation } from 'react-i18next';
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from '../locales/i18n';

const LABELS: Record<SupportedLanguage, string> = {
  en: 'EN',
  mk: 'МК',
};

export function LanguageSwitcher() {
  const { i18n } = useTranslation();
  const current = (i18n.resolvedLanguage ?? 'en') as SupportedLanguage;

  return (
    <div className="inline-flex overflow-hidden rounded-lg border border-gray-200 bg-white text-xs font-medium">
      {SUPPORTED_LANGUAGES.map((lng) => {
        const active = current === lng;
        return (
          <button
            key={lng}
            type="button"
            onClick={() => void i18n.changeLanguage(lng)}
            className={
              active
                ? 'px-2.5 py-1.5 bg-brand-600 text-white'
                : 'px-2.5 py-1.5 text-gray-600 hover:bg-gray-50'
            }
          >
            {LABELS[lng]}
          </button>
        );
      })}
    </div>
  );
}
