import { useTranslation } from 'react-i18next';
import { notificationTypeClasses, notificationTypeKey } from '../utils/labels';
import { NotificationType } from '../types';

interface NotificationIconProps {
  type: number;
}

// One SVG path per notification category, keyed by the NotificationType ordinal.
const ICON_PATHS: Record<number, string> = {
  [NotificationType.Family]:
    'M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z',
  [NotificationType.Task]:
    'M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
  [NotificationType.Calendar]:
    'M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5',
  [NotificationType.Shopping]:
    'M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M7.5 14.25a3 3 0 00-3 3h15.75m-12.75-3h11.218c1.121-2.3 2.1-4.684 2.924-7.138a60.114 60.114 0 00-16.536-1.84M7.5 14.25L5.106 5.272M6 20.25a.75.75 0 11-1.5 0 .75.75 0 011.5 0zm12.75 0a.75.75 0 11-1.5 0 .75.75 0 011.5 0z',
  [NotificationType.FamilyVault]:
    'M12 15v2.25m0 0a2.25 2.25 0 002.25-2.25V9a2.25 2.25 0 00-4.5 0v6A2.25 2.25 0 0012 17.25zM6.75 21h10.5a2.25 2.25 0 002.25-2.25v-7.5a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v7.5A2.25 2.25 0 006.75 21z',
  [NotificationType.System]:
    'M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z',
};

export function NotificationIcon({ type }: NotificationIconProps) {
  const { t } = useTranslation();
  const path = ICON_PATHS[type] ?? ICON_PATHS[NotificationType.System];

  return (
    <span
      className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full ${notificationTypeClasses(type)}`}
      role="img"
      aria-label={t(notificationTypeKey(type))}
    >
      <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" d={path} />
      </svg>
    </span>
  );
}
