import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { navItems } from './navigation';

export function Sidebar() {
  const { t } = useTranslation();

  return (
    <aside className="hidden w-64 shrink-0 flex-col border-r border-gray-200 bg-white md:flex">
      <div className="flex h-16 items-center gap-2 border-b border-gray-200 px-6">
        <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-sm font-bold text-white">
          FH
        </span>
        <span className="text-lg font-semibold text-gray-900">{t('app.name')}</span>
      </div>

      <nav className="flex-1 space-y-1 px-3 py-4">
        {navItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            className={({ isActive }) =>
              [
                'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-brand-50 text-brand-700'
                  : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
              ].join(' ')
            }
          >
            {item.icon}
            <span>{t(item.labelKey)}</span>
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
