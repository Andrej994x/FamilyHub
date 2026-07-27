import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { applyUpdate, onUpdateAvailable } from '../pwa/serviceWorkerRegistration';

export function UpdateNotification() {
  const { t } = useTranslation();
  const [available, setAvailable] = useState(false);
  const [reloading, setReloading] = useState(false);

  useEffect(() => onUpdateAvailable(() => setAvailable(true)), []);

  if (!available) {
    return null;
  }

  const reload = () => {
    setReloading(true);
    // Activates the waiting worker; the controllerchange handler then reloads the page.
    applyUpdate();
  };

  return (
    <div className="fixed inset-x-0 bottom-24 z-50 flex justify-center px-4 md:bottom-6 md:justify-center md:px-6">
      <div className="flex w-full max-w-sm items-center gap-3 rounded-xl bg-gray-900 p-3 pl-4 text-white shadow-lg">
        <p className="min-w-0 flex-1 text-sm">{t('pwa.update.message')}</p>
        <button
          type="button"
          onClick={reload}
          disabled={reloading}
          className="shrink-0 rounded-lg bg-white px-3 py-1.5 text-xs font-semibold text-gray-900 hover:bg-gray-100 disabled:opacity-70"
        >
          {reloading ? t('pwa.update.reloading') : t('pwa.update.action')}
        </button>
      </div>
    </div>
  );
}
