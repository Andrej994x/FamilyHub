import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

/** The non-standard event fired by Chromium browsers when the app is installable. */
interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

const DISMISS_KEY = 'familyhub_install_dismissed';

export function InstallPrompt() {
  const { t } = useTranslation();
  const [promptEvent, setPromptEvent] = useState<BeforeInstallPromptEvent | null>(null);

  useEffect(() => {
    // Respect a previous dismissal and don't offer install when already running standalone.
    const dismissed = localStorage.getItem(DISMISS_KEY) === '1';
    const standalone =
      window.matchMedia('(display-mode: standalone)').matches ||
      // iOS Safari exposes standalone on the navigator instead of matchMedia.
      (window.navigator as unknown as { standalone?: boolean }).standalone === true;
    if (dismissed || standalone) {
      return;
    }

    const onBeforeInstall = (event: Event) => {
      // Keep the browser's default mini-infobar from showing; we present our own UI.
      event.preventDefault();
      setPromptEvent(event as BeforeInstallPromptEvent);
    };
    const onInstalled = () => {
      setPromptEvent(null);
      localStorage.setItem(DISMISS_KEY, '1');
    };

    window.addEventListener('beforeinstallprompt', onBeforeInstall);
    window.addEventListener('appinstalled', onInstalled);
    return () => {
      window.removeEventListener('beforeinstallprompt', onBeforeInstall);
      window.removeEventListener('appinstalled', onInstalled);
    };
  }, []);

  if (!promptEvent) {
    return null;
  }

  const install = async () => {
    await promptEvent.prompt();
    await promptEvent.userChoice;
    // The prompt can only be used once; drop it whatever the user chose.
    setPromptEvent(null);
  };

  const dismiss = () => {
    localStorage.setItem(DISMISS_KEY, '1');
    setPromptEvent(null);
  };

  return (
    <div className="fixed inset-x-0 bottom-24 z-50 flex justify-center px-4 md:bottom-6 md:justify-end md:px-6">
      <div className="flex w-full max-w-sm items-center gap-3 rounded-xl border border-gray-200 bg-white p-3 shadow-lg">
        <img src="/icons/icon-192.png" alt="" className="h-10 w-10 shrink-0 rounded-lg" />
        <div className="min-w-0 flex-1">
          <p className="text-sm font-semibold text-gray-900">{t('app.name')}</p>
          <p className="truncate text-xs text-gray-500">{t('pwa.install.message')}</p>
        </div>
        <div className="flex shrink-0 items-center gap-1.5">
          <button
            type="button"
            onClick={dismiss}
            className="rounded-lg px-2.5 py-1.5 text-xs font-medium text-gray-500 hover:bg-gray-100"
          >
            {t('pwa.install.dismiss')}
          </button>
          <button
            type="button"
            onClick={() => void install()}
            className="rounded-lg bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-brand-700"
          >
            {t('pwa.install.action')}
          </button>
        </div>
      </div>
    </div>
  );
}
