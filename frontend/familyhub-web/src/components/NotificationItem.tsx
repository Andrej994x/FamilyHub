import { useTranslation } from 'react-i18next';
import { formatTimestampShort } from '../utils/date';
import { notificationRoute } from '../utils/labels';
import type { NotificationResponse } from '../types';

interface NotificationItemProps {
  notification: NotificationResponse;
  onActivate: (notification: NotificationResponse) => void;
}

export function NotificationItem({ notification, onActivate }: NotificationItemProps) {
  const { i18n } = useTranslation();
  const hasLink = notificationRoute(notification.type) !== null;

  return (
    <button
      type="button"
      onClick={() => onActivate(notification)}
      className={`flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-gray-50 ${
        notification.isRead ? '' : 'bg-brand-50/40'
      }`}
    >
      {/* Unread indicator keeps a fixed column so titles stay aligned. */}
      <span className="mt-1.5 flex h-2 w-2 shrink-0 items-center justify-center">
        {!notification.isRead && <span className="h-2 w-2 rounded-full bg-brand-600" />}
      </span>

      <span className="min-w-0 flex-1">
        <span className="flex items-baseline justify-between gap-2">
          <span
            className={`truncate text-sm ${
              notification.isRead ? 'font-medium text-gray-700' : 'font-semibold text-gray-900'
            }`}
          >
            {notification.title}
          </span>
          <span className="shrink-0 text-xs text-gray-400">
            {formatTimestampShort(notification.createdAt, i18n.language)}
          </span>
        </span>
        <span className="mt-0.5 block text-sm text-gray-500">{notification.message}</span>
      </span>

      {hasLink && (
        <svg
          className="mt-1 h-4 w-4 shrink-0 text-gray-300"
          fill="none"
          viewBox="0 0 24 24"
          strokeWidth={1.7}
          stroke="currentColor"
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
        </svg>
      )}
    </button>
  );
}
