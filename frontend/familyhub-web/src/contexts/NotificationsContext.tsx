import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import type { NotificationResponse } from '../types';
import { notificationService } from '../services/notificationService';

export interface NotificationsContextValue {
  notifications: NotificationResponse[];
  unreadCount: number;
  loading: boolean;
  refresh: () => Promise<void>;
  markAsRead: (id: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
}

export const NotificationsContext = createContext<NotificationsContextValue | undefined>(undefined);

/** How often the notification list is silently refreshed to keep the badge current. */
const POLL_INTERVAL_MS = 60_000;

export function NotificationsProvider({ children }: { children: ReactNode }) {
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  // Guards against overlapping refreshes from the poll and manual triggers.
  const inFlight = useRef(false);

  const refresh = useCallback(async () => {
    if (inFlight.current) {
      return;
    }
    inFlight.current = true;
    try {
      setNotifications(await notificationService.list());
    } catch {
      // Keep the last known list; a later poll or action will retry.
    } finally {
      inFlight.current = false;
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
    const timer = window.setInterval(() => void refresh(), POLL_INTERVAL_MS);
    return () => window.clearInterval(timer);
  }, [refresh]);

  const markAsRead = useCallback(async (id: string) => {
    // Optimistic: flip locally, then persist. Revert via refresh on failure.
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
    );
    try {
      await notificationService.markAsRead(id);
    } catch {
      await notificationService.list().then(setNotifications).catch(() => {});
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    try {
      await notificationService.markAllAsRead();
    } catch {
      await notificationService.list().then(setNotifications).catch(() => {});
    }
  }, []);

  const unreadCount = useMemo(
    () => notifications.reduce((count, n) => (n.isRead ? count : count + 1), 0),
    [notifications],
  );

  const value: NotificationsContextValue = {
    notifications,
    unreadCount,
    loading,
    refresh,
    markAsRead,
    markAllAsRead,
  };

  return (
    <NotificationsContext.Provider value={value}>{children}</NotificationsContext.Provider>
  );
}
