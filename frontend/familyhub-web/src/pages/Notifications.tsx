import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useNotifications } from '../hooks/useNotifications';
import { NotificationItem } from '../components/NotificationItem';
import type { NotificationResponse } from '../types';

type Tab = 'all' | 'unread' | 'read';

/** How many notifications to show per page (keeps long lists manageable). */
const PAGE_SIZE = 10;

export default function Notifications() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const {
    notifications,
    unreadCount,
    loading,
    error,
    refresh,
    markAsRead,
    markAllAsRead,
    deleteOne,
    deleteAllRead,
  } = useNotifications();

  const [tab, setTab] = useState<Tab>('all');
  const [page, setPage] = useState(1);

  const readCount = notifications.length - unreadCount;

  const visible = useMemo(() => {
    switch (tab) {
      case 'unread':
        return notifications.filter((n) => !n.isRead);
      case 'read':
        return notifications.filter((n) => n.isRead);
      default:
        return notifications;
    }
  }, [notifications, tab]);

  // Reset to the first page whenever the tab changes.
  useEffect(() => {
    setPage(1);
  }, [tab]);

  const totalPages = Math.max(1, Math.ceil(visible.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages); // clamp if the list shrank
  const pageItems = visible.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const handleActivate = (notification: NotificationResponse) => {
    if (!notification.isRead) {
      void markAsRead(notification.id);
    }
    if (notification.relatedUrl) {
      navigate(notification.relatedUrl);
    }
  };

  const handleDeleteAllRead = () => {
    if (readCount === 0 || !window.confirm(t('notifications.deleteAllReadConfirm'))) {
      return;
    }
    void deleteAllRead();
  };

  const tabs: { key: Tab; label: string; count: number }[] = [
    { key: 'all', label: t('notifications.tabs.all'), count: notifications.length },
    { key: 'unread', label: t('notifications.tabs.unread'), count: unreadCount },
    { key: 'read', label: t('notifications.tabs.read'), count: readCount },
  ];

  const emptyMessage =
    tab === 'unread'
      ? t('notifications.emptyUnread')
      : tab === 'read'
        ? t('notifications.emptyRead')
        : t('notifications.empty');

  const isInitialLoading = loading && notifications.length === 0;
  const isError = error && notifications.length === 0;

  return (
    <div className="mx-auto max-w-2xl">
      <header className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('notifications.title')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('notifications.subtitle')}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {unreadCount > 0 && (
            <button
              type="button"
              onClick={() => void markAllAsRead()}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              {t('notifications.markAllRead')}
            </button>
          )}
          {readCount > 0 && (
            <button
              type="button"
              onClick={handleDeleteAllRead}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 hover:text-red-600"
            >
              {t('notifications.deleteAllRead')}
            </button>
          )}
        </div>
      </header>

      {/* Tabs */}
      <div className="mb-4 flex gap-1 overflow-x-auto rounded-lg bg-gray-100 p-1">
        {tabs.map((item) => (
          <button
            key={item.key}
            type="button"
            onClick={() => setTab(item.key)}
            className={`flex-1 whitespace-nowrap rounded-md px-3 py-1.5 text-sm font-medium transition-colors ${
              tab === item.key ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            {item.label}
            <span className={`ml-1.5 text-xs ${tab === item.key ? 'text-brand-600' : 'text-gray-400'}`}>
              {item.count}
            </span>
          </button>
        ))}
      </div>

      {isInitialLoading ? (
        <div className="rounded-xl border border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {t('common.loading')}
        </div>
      ) : isError ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-gray-200 bg-white p-10 text-center">
          <p className="text-sm text-gray-500">{t('notifications.loadError')}</p>
          <button
            type="button"
            onClick={() => void refresh()}
            className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : visible.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {emptyMessage}
        </div>
      ) : (
        <>
          <div className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
            {pageItems.map((notification) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onActivate={handleActivate}
                onDelete={(n) => void deleteOne(n.id)}
              />
            ))}
          </div>

          {totalPages > 1 && (
            <nav
              className="mt-4 flex items-center justify-between gap-3"
              aria-label={t('notifications.pagination.label')}
            >
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={currentPage <= 1}
                className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-40"
              >
                {t('notifications.pagination.prev')}
              </button>
              <span className="text-xs text-gray-500">
                {t('notifications.pagination.status', {
                  page: currentPage,
                  total: totalPages,
                  count: visible.length,
                })}
              </span>
              <button
                type="button"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={currentPage >= totalPages}
                className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-40"
              >
                {t('notifications.pagination.next')}
              </button>
            </nav>
          )}
        </>
      )}
    </div>
  );
}
