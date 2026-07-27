import { useCallback, useEffect, useState } from 'react';
import {
  getExistingSubscription,
  getPermission,
  isPushSupported,
  subscribeToPush,
  syncSubscription,
  unsubscribeFromPush,
} from '../pwa/push';

/** A semantic error code the UI maps to a message; null when there is no error. */
export type PushError = 'generic' | 'notConfigured' | null;

export interface PushNotificationsState {
  supported: boolean;
  permission: NotificationPermission;
  subscribed: boolean;
  busy: boolean;
  error: PushError;
  enable: () => Promise<void>;
  disable: () => Promise<void>;
}

export function usePushNotifications(): PushNotificationsState {
  const [supported] = useState(isPushSupported);
  const [permission, setPermission] = useState<NotificationPermission>(getPermission);
  const [subscribed, setSubscribed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<PushError>(null);

  // Reflect the current subscription on mount and keep the backend in sync.
  useEffect(() => {
    if (!supported) {
      return;
    }
    let cancelled = false;
    void (async () => {
      const subscription = await getExistingSubscription().catch(() => null);
      if (!cancelled) {
        setSubscribed(Boolean(subscription));
      }
      if (subscription) {
        void syncSubscription();
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [supported]);

  const enable = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const result = await subscribeToPush();
      setPermission(getPermission());
      if (result === 'subscribed') {
        setSubscribed(true);
      } else if (result === 'not-configured') {
        setError('notConfigured');
      }
      // 'denied'/'unsupported' are conveyed by the permission/supported state, not an error.
    } catch {
      setError('generic');
    } finally {
      setBusy(false);
    }
  }, []);

  const disable = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      await unsubscribeFromPush();
      setSubscribed(false);
    } catch {
      setError('generic');
    } finally {
      setBusy(false);
    }
  }, []);

  return { supported, permission, subscribed, busy, error, enable, disable };
}
