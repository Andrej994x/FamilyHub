import type { ReactNode } from 'react';

interface StatTileProps {
  label: string;
  value: ReactNode;
  hint?: string;
  icon: ReactNode;
  onClick?: () => void;
}

export function StatTile({ label, value, hint, icon, onClick }: StatTileProps) {
  const content = (
    <>
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-gray-500">{label}</p>
        <span className="text-brand-600">{icon}</span>
      </div>
      <p className="mt-2 text-2xl font-semibold text-gray-900">{value}</p>
      {hint && <p className="mt-0.5 text-xs text-gray-400">{hint}</p>}
    </>
  );

  const className =
    'rounded-2xl border border-gray-200 bg-white p-5 text-left transition-colors';

  if (onClick) {
    return (
      <button type="button" onClick={onClick} className={`${className} hover:border-brand-300 hover:bg-brand-50/30`}>
        {content}
      </button>
    );
  }

  return <div className={className}>{content}</div>;
}
