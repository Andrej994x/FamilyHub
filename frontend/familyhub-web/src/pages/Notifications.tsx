import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useNotifications } from '../hooks/useNotifications';
import { notificationRoute } from '../utils/labels';
import { NotificationItem } from '../components/NotificationItem';
import type { NotificationResponse } from '../types';

export default function Notifications() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { notifications, unreadCount, loading, markAsRead, markAllAsRead } = useNotifications();

  const handleActivate = (notification: NotificationResponse) => {
    if (!notification.isRead) {
      void markAsRead(notification.id);
    }
    const route = notificationRoute(notification.type);
    if (route) {
      navigate(route);
    }
  };

  return (
    <div className="mx-auto max-w-2xl">
      <header className="mb-6 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('notifications.title')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('notifications.subtitle')}</p>
        </div>
        {unreadCount > 0 && (
          <button
            type="button"
            onClick={() => void markAllAsRead()}
            className="shrink-0 rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {t('notifications.markAllRead')}
          </button>
        )}
      </header>

      {loading && notifications.length === 0 ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : notifications.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {t('notifications.empty')}
        </div>
      ) : (
        <div className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
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
  );
}
