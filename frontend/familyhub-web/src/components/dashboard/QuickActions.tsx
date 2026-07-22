import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

interface QuickAction {
  labelKey: string;
  to: string;
  icon: ReactNode;
}

const iconProps = {
  className: 'h-5 w-5',
  fill: 'none',
  viewBox: '0 0 24 24',
  strokeWidth: 1.7,
  stroke: 'currentColor',
} as const;

const actions: QuickAction[] = [
  {
    labelKey: 'dashboard.quick.addEvent',
    to: '/calendar',
    icon: (
      <svg {...iconProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 8.25h18M3 8.25A2.25 2.25 0 015.25 6h13.5A2.25 2.25 0 0121 8.25v10.5A2.25 2.25 0 0118.75 21H5.25A2.25 2.25 0 013 18.75V8.25z" />
      </svg>
    ),
  },
  {
    labelKey: 'dashboard.quick.addTask',
    to: '/tasks',
    icon: (
      <svg {...iconProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
  },
  {
    labelKey: 'dashboard.quick.addShopping',
    to: '/shopping',
    icon: (
      <svg {...iconProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M4.5 5.25h15l-1.5 8.25H6L4.5 5.25zm2 12a.75.75 0 100 1.5.75.75 0 000-1.5zm10.5 0a.75.75 0 100 1.5.75.75 0 000-1.5z" />
      </svg>
    ),
  },
  {
    labelKey: 'dashboard.quick.addPickup',
    to: '/pickups',
    icon: (
      <svg {...iconProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z" />
        <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1115 0z" />
      </svg>
    ),
  },
];

export function QuickActions() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
      {actions.map((action) => (
        <button
          key={action.to}
          type="button"
          onClick={() => navigate(action.to)}
          className="flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-4 py-3 text-sm font-medium text-gray-700 transition-colors hover:border-brand-300 hover:bg-brand-50/40"
        >
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
            {action.icon}
          </span>
          <span className="truncate">{t(action.labelKey)}</span>
        </button>
      ))}
    </div>
  );
}
