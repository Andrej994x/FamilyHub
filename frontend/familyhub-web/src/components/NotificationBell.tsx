import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useNotifications } from '../hooks/useNotifications';
import { notificationRoute } from '../utils/labels';
import { NotificationItem } from './NotificationItem';
import type { NotificationResponse } from '../types';

export function NotificationBell() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { notifications, unreadCount, markAsRead, markAllAsRead, refresh } = useNotifications();

  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Close the desktop dropdown on outside click or Escape.
  useEffect(() => {
    if (!open) {
      return;
    }
    const onPointerDown = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('mousedown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  const isDesktop = () => window.matchMedia('(min-width: 768px)').matches;

  const handleBellClick = () => {
    // Desktop opens a dropdown; mobile routes to the full-page list.
    if (isDesktop()) {
      const next = !open;
      setOpen(next);
      if (next) {
        void refresh();
      }
    } else {
      navigate('/notifications');
    }
  };

  const handleActivate = (notification: NotificationResponse) => {
    if (!notification.isRead) {
      void markAsRead(notification.id);
    }
    setOpen(false);
    const route = notificationRoute(notification.type);
    if (route) {
      navigate(route);
    }
  };

  const badge = unreadCount > 9 ? '9+' : String(unreadCount);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={handleBellClick}
        aria-label={t('notifications.title')}
        aria-haspopup="true"
        aria-expanded={open}
        className="relative rounded-lg p-2 text-gray-600 hover:bg-gray-100"
      >
        <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0"
          />
        </svg>
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-semibold leading-none text-white">
            {badge}
          </span>
        )}
      </button>

      {/* Desktop dropdown — never shown on mobile (bell navigates there instead). */}
      {open && (
        <div className="absolute right-0 z-30 mt-2 hidden w-80 overflow-hidden rounded-xl border border-gray-200 bg-white shadow-xl md:block">
          <div className="flex items-center justify-between border-b border-gray-100 px-4 py-3">
            <h2 className="text-sm font-semibold text-gray-900">{t('notifications.title')}</h2>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() => void markAllAsRead()}
                className="text-xs font-medium text-brand-600 hover:underline"
              >
                {t('notifications.markAllRead')}
              </button>
            )}
          </div>

          {notifications.length === 0 ? (
            <p className="px-4 py-8 text-center text-sm text-gray-400">{t('notifications.empty')}</p>
          ) : (
            <div className="max-h-96 divide-y divide-gray-100 overflow-y-auto">
              {notifications.map((notification) => (
                <NotificationItem
                  key={notification.id}
                  notification={notification}
                  onActivate={handleActivate}
                />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
