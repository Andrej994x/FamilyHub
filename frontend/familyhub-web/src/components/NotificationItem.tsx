import { useTranslation } from 'react-i18next';
import { formatTimestampShort } from '../utils/date';
import { NotificationIcon } from './NotificationIcon';
import type { NotificationResponse } from '../types';

interface NotificationItemProps {
  notification: NotificationResponse;
  onActivate: (notification: NotificationResponse) => void;
  /** When provided, a delete button is shown (used on the full page, not the preview). */
  onDelete?: (notification: NotificationResponse) => void;
}

export function NotificationItem({ notification, onActivate, onDelete }: NotificationItemProps) {
  const { t, i18n } = useTranslation();
  const hasLink = Boolean(notification.relatedUrl);

  return (
    <div
      className={`group relative flex items-start gap-3 px-4 py-3 transition-colors hover:bg-gray-50 ${
        notification.isRead ? '' : 'bg-brand-50/40'
      }`}
    >
      <NotificationIcon type={notification.type} />

      <button
        type="button"
        onClick={() => onActivate(notification)}
        className="min-w-0 flex-1 text-left"
      >
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
        {hasLink && (
          <span className="mt-1 inline-flex items-center gap-1 text-xs font-medium text-brand-600">
            {t('notifications.view')}
            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
            </svg>
          </span>
        )}
      </button>

      <div className="flex shrink-0 flex-col items-end gap-2 pl-1">
        {!notification.isRead && (
          <span
            className="mt-1 h-2 w-2 rounded-full bg-brand-600"
            aria-label={t('notifications.unread')}
          />
        )}
        {onDelete && (
          <button
            type="button"
            onClick={() => onDelete(notification)}
            aria-label={t('common.delete')}
            className="rounded-md p-1 text-gray-300 hover:bg-gray-100 hover:text-red-600 md:opacity-0 md:group-hover:opacity-100"
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0"
              />
            </svg>
          </button>
        )}
      </div>
    </div>
  );
}
