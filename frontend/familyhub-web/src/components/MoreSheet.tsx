import { useEffect } from 'react';
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { LanguageSwitcher } from './LanguageSwitcher';
import type { NavItem } from './navigation';

interface MoreSheetProps {
  open: boolean;
  onClose: () => void;
  items: NavItem[];
}

export function MoreSheet({ open, onClose, items }: MoreSheetProps) {
  const { t } = useTranslation();
  const { logout } = useAuth();

  // Lock background scroll and support Escape while the sheet is open.
  useEffect(() => {
    if (!open) {
      return;
    }
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKey);
    return () => {
      document.body.style.overflow = previous;
      document.removeEventListener('keydown', onKey);
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-40 flex items-end bg-black/40 md:hidden"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="w-full rounded-t-2xl bg-white pb-[calc(env(safe-area-inset-bottom)+1rem)] shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Drag affordance */}
        <div className="flex justify-center pt-3">
          <span className="h-1.5 w-10 rounded-full bg-gray-300" />
        </div>

        <div className="flex items-center justify-between px-5 pb-2 pt-3">
          <h2 className="text-base font-semibold text-gray-900">{t('nav.more')}</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('common.close')}
            className="rounded-lg p-1.5 text-gray-400 hover:bg-gray-100"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <nav className="px-3 pb-2">
          {items.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              onClick={onClose}
              className={({ isActive }) =>
                [
                  'flex items-center gap-3 rounded-xl px-3 py-3 text-sm font-medium transition-colors',
                  isActive ? 'bg-brand-50 text-brand-700' : 'text-gray-700 active:bg-gray-100',
                ].join(' ')
              }
            >
              {item.icon}
              <span>{t(item.labelKey)}</span>
            </NavLink>
          ))}
        </nav>

        <div className="mt-1 flex items-center justify-between gap-3 border-t border-gray-100 px-5 py-4">
          <LanguageSwitcher />
          <button
            type="button"
            onClick={() => {
              onClose();
              logout();
            }}
            className="rounded-lg border border-gray-200 px-3 py-2 text-sm font-medium text-gray-600 active:bg-gray-50"
          >
            {t('common.logout')}
          </button>
        </div>
      </div>
    </div>
  );
}
