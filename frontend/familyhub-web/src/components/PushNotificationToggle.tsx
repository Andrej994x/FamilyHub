import { useTranslation } from 'react-i18next';
import { usePushNotifications } from '../hooks/usePushNotifications';

/** Is this an iOS device that is not yet installed to the Home Screen? */
function isIosNeedingInstall(): boolean {
  const ua = navigator.userAgent;
  const isIos = /iphone|ipad|ipod/i.test(ua);
  const standalone =
    window.matchMedia('(display-mode: standalone)').matches ||
    (window.navigator as unknown as { standalone?: boolean }).standalone === true;
  return isIos && !standalone;
}

export function PushNotificationToggle() {
  const { t } = useTranslation();
  const { supported, permission, subscribed, busy, error, enable, disable } = usePushNotifications();

  const blocked = permission === 'denied';
  const canToggle = supported && !blocked;

  // A short status line under the title explaining the current state.
  let hint: string | null = null;
  if (!supported) {
    hint = isIosNeedingInstall() ? t('notifications.push.iosHint') : t('notifications.push.unsupported');
  } else if (blocked) {
    hint = t('notifications.push.deniedHint');
  } else if (error === 'notConfigured') {
    hint = t('notifications.push.notConfigured');
  } else if (error === 'generic') {
    hint = t('notifications.push.error');
  } else {
    hint = subscribed ? t('notifications.push.descriptionOn') : t('notifications.push.descriptionOff');
  }

  const toggle = () => {
    if (busy) {
      return;
    }
    void (subscribed ? disable() : enable());
  };

  return (
    <div className="rounded-xl border border-gray-200 bg-white p-4">
      <div className="flex items-center justify-between gap-4">
        <div className="min-w-0">
          <h2 className="text-sm font-semibold text-gray-900">{t('notifications.push.title')}</h2>
          <p
            className={`mt-0.5 text-xs ${
              blocked || error ? 'text-amber-600' : 'text-gray-500'
            }`}
          >
            {hint}
          </p>
        </div>

        {canToggle ? (
          <button
            type="button"
            role="switch"
            aria-checked={subscribed}
            aria-label={t('notifications.push.title')}
            aria-busy={busy}
            onClick={toggle}
            disabled={busy}
            className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors disabled:opacity-60 ${
              subscribed ? 'bg-brand-600' : 'bg-gray-300'
            }`}
          >
            <span
              className={`inline-block h-5 w-5 transform rounded-full bg-white shadow transition-transform ${
                subscribed ? 'translate-x-5' : 'translate-x-0.5'
              }`}
            />
          </button>
        ) : (
          <span className="shrink-0 rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-500">
            {blocked ? t('notifications.push.blockedLabel') : t('notifications.push.unavailableLabel')}
          </span>
        )}
      </div>
    </div>
  );
}
