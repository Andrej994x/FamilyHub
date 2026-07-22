import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { navItems } from './navigation';

export function BottomNav() {
  const { t } = useTranslation();

  return (
    <nav className="fixed inset-x-0 bottom-0 z-20 flex border-t border-gray-200 bg-white md:hidden">
      {navItems.map((item) => (
        <NavLink
          key={item.path}
          to={item.path}
          className={({ isActive }) =>
            [
              'flex flex-1 flex-col items-center gap-0.5 py-2 text-[10px] font-medium transition-colors',
              isActive ? 'text-brand-600' : 'text-gray-500 hover:text-gray-800',
            ].join(' ')
          }
        >
          {item.icon}
          <span className="truncate">{t(item.labelKey)}</span>
        </NavLink>
      ))}
    </nav>
  );
}
