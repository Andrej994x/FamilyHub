import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useFamily } from '../../hooks/useFamily';
import { canAccessExpenses } from '../../utils/roles';

interface QuickAction {
  labelKey: string;
  to: string;
  icon: ReactNode;
  /** Hides the action for roles that can't use it (cosmetic; the API enforces access). */
  visible?: (role: number | null) => boolean;
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
  },  {
    labelKey: 'dashboard.quick.addExpense',
    to: '/expenses?add=1',
    visible: canAccessExpenses,
    icon: (
      <svg {...iconProps}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v12m-3-2.818l.879.659c1.171.879 3.07.879 4.242 0 1.172-.879 1.172-2.303 0-3.182C13.536 12.219 12.768 12 12 12c-.725 0-1.45-.22-2.003-.659-1.106-.879-1.106-2.303 0-3.182s2.9-.879 4.006 0l.415.33M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
  },
];

export function QuickActions() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { role } = useFamily();
  const visible = actions.filter((action) => !action.visible || action.visible(role));
  // Five actions don't fit a 4-column row; on the 2-column phone grid an odd last one spans the row.
  const columns = visible.length === 5 ? 'sm:grid-cols-3 lg:grid-cols-5' : 'sm:grid-cols-4';

  return (
    <div className={`grid grid-cols-2 gap-3 ${columns}`}>
      {visible.map((action, index) => (
        <button
          key={action.to}
          type="button"
          onClick={() => navigate(action.to)}
          className={`flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-4 py-3 text-sm font-medium text-gray-700 transition-colors hover:border-brand-300 hover:bg-brand-50/40 ${
            visible.length % 2 === 1 && index === visible.length - 1 ? 'col-span-2 sm:col-span-1' : ''
          }`}
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
