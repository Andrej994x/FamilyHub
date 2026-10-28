import { useTranslation } from 'react-i18next';
import { addMonths, compareMonths, formatMonthYear, type YearMonth } from '../../utils/month';

interface MonthSwitcherProps {
  value: YearMonth;
  onChange: (value: YearMonth) => void;
  /** The latest month that can be selected; omit for no limit (budgets can be planned ahead). */
  max?: YearMonth;
}

const buttonClass =
  'flex h-9 w-9 items-center justify-center rounded-lg border border-gray-200 bg-white text-gray-600 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40';

export function MonthSwitcher({ value, onChange, max }: MonthSwitcherProps) {
  const { t, i18n } = useTranslation();
  const atMax = max !== undefined && compareMonths(value, max) >= 0;

  return (
    <div className="flex items-center gap-2">
      <button
        type="button"
        onClick={() => onChange(addMonths(value, -1))}
        aria-label={t('expenses.month.previous')}
        className={buttonClass}
      >
        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
        </svg>
      </button>
      <p className="min-w-[10rem] text-center text-sm font-semibold text-gray-900" aria-live="polite">
        {formatMonthYear(value, i18n.language)}
      </p>
      <button
        type="button"
        onClick={() => onChange(addMonths(value, 1))}
        disabled={atMax}
        aria-label={t('expenses.month.next')}
        className={buttonClass}
      >
        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
        </svg>
      </button>
    </div>
  );
}
