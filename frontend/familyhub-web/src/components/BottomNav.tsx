import { useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { navItems } from './navigation';
import { MoreSheet } from './MoreSheet';

// The bottom bar shows the daily-activity destinations; less-frequent ones live in "More".
const PRIMARY_PATHS = ['/dashboard', '/calendar', '/tasks', '/shopping', '/pickups'];

const primaryItems = PRIMARY_PATHS.map((path) => navItems.find((i) => i.path === path)).filter(
  (i): i is (typeof navItems)[number] => Boolean(i),
);
const overflowItems = navItems.filter((i) => !PRIMARY_PATHS.includes(i.path));

const cellClass = 'flex flex-1 flex-col items-center justify-center gap-1 py-1.5';

export function BottomNav() {
  const { t } = useTranslation();
  const location = useLocation();
  const [moreOpen, setMoreOpen] = useState(false);

  const moreActive = overflowItems.some((i) => location.pathname.startsWith(i.path));

  return (
    <>
      <nav className="pb-safe fixed inset-x-0 bottom-0 z-30 flex border-t border-gray-200 bg-white/95 backdrop-blur md:hidden">
        {primaryItems.map((item) => (
          <NavLink key={item.path} to={item.path} className={cellClass}>
            {({ isActive }) => (
              <>
                <span
                  className={`flex h-8 w-14 items-center justify-center rounded-full transition-colors ${
                    isActive ? 'bg-brand-100 text-brand-700' : 'text-gray-500'
                  }`}
                >
                  {item.icon}
                </span>
                <span
                  className={`max-w-full truncate text-[10px] font-medium ${
                    isActive ? 'text-brand-700' : 'text-gray-500'
                  }`}
                >
                  {t(item.labelKey)}
                </span>
              </>
            )}
          </NavLink>
        ))}

        <button type="button" onClick={() => setMoreOpen(true)} className={cellClass}>
          <span
            className={`flex h-8 w-14 items-center justify-center rounded-full transition-colors ${
              moreActive ? 'bg-brand-100 text-brand-700' : 'text-gray-500'
            }`}
          >
            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
            </svg>
          </span>
          <span
            className={`text-[10px] font-medium ${moreActive ? 'text-brand-700' : 'text-gray-500'}`}
          >
            {t('nav.more')}
          </span>
        </button>
      </nav>

      <MoreSheet open={moreOpen} onClose={() => setMoreOpen(false)} items={overflowItems} />
    </>
  );
}
