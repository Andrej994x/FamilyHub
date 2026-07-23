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
  /** Set when the last load failed and no data could be shown. */
  error: boolean;
  refresh: () => Promise<void>;
  markAsRead: (id: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  deleteOne: (id: string) => Promise<void>;
  deleteAllRead: () => Promise<void>;
}

export const NotificationsContext = createContext<NotificationsContextValue | undefined>(undefined);

/** How often the notification list is silently refreshed to keep the badge current. */
const POLL_INTERVAL_MS = 60_000;

export function NotificationsProvider({ children }: { children: ReactNode }) {
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  // Guards against overlapping refreshes from the poll and manual triggers.
  const inFlight = useRef(false);

  const reload = useCallback(async () => {
    try {
      setNotifications(await notificationService.list());
    } catch {
      // Swallow — used to reconcile after a failed optimistic mutation.
    }
  }, []);

  const refresh = useCallback(async () => {
    if (inFlight.current) {
      return;
    }
    inFlight.current = true;
    try {
      setNotifications(await notificationService.list());
      setError(false);
    } catch {
      // Only surface an error when there is nothing to show; otherwise keep the
      // last known list and let a later poll or action retry.
      setNotifications((prev) => {
        if (prev.length === 0) {
          setError(true);
        }
        return prev;
      });
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

  const markAsRead = useCallback(
    async (id: string) => {
      // Optimistic: flip locally, then persist. Reconcile on failure.
      setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
      try {
        await notificationService.markAsRead(id);
      } catch {
        await reload();
      }
    },
    [reload],
  );

  const markAllAsRead = useCallback(async () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    try {
      await notificationService.markAllAsRead();
    } catch {
      await reload();
    }
  }, [reload]);

  const deleteOne = useCallback(
    async (id: string) => {
      setNotifications((prev) => prev.filter((n) => n.id !== id));
      try {
        await notificationService.remove(id);
      } catch {
        await reload();
      }
    },
    [reload],
  );

  const deleteAllRead = useCallback(async () => {
    setNotifications((prev) => prev.filter((n) => !n.isRead));
    try {
      await notificationService.deleteAllRead();
    } catch {
      await reload();
    }
  }, [reload]);

  const unreadCount = useMemo(
    () => notifications.reduce((count, n) => (n.isRead ? count : count + 1), 0),
    [notifications],
  );

  const value: NotificationsContextValue = {
    notifications,
    unreadCount,
    loading,
    error,
    refresh,
    markAsRead,
    markAllAsRead,
    deleteOne,
    deleteAllRead,
  };

  return (
    <NotificationsContext.Provider value={value}>{children}</NotificationsContext.Provider>
  );
}
