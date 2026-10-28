import { useTranslation } from 'react-i18next';
import type { ExpenseSummaryResponse } from '../../types';
import { formatMoney } from '../../utils/money';
import { usageColor } from '../../utils/expenseCategories';

interface BudgetOverviewProps {
  summary: ExpenseSummaryResponse;
}

/** The month's spending against the family budget, or the spending alone when there is no budget. */
export function BudgetOverview({ summary }: BudgetOverviewProps) {
  const { t, i18n } = useTranslation();
  const money = (amount: number) => formatMoney(amount, i18n.language);
  const { totalSpent, budget, remaining, usagePercent, expenseCount } = summary;
  const overBudget = remaining !== null && remaining < 0;

  return (
    <section className="rounded-2xl border border-gray-200 bg-white p-5" aria-label={t('expenses.summary.title')}>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div>
          <p className="text-sm font-medium text-gray-500">{t('expenses.summary.spent')}</p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">{money(totalSpent)}</p>
          <p className="mt-0.5 text-xs text-gray-400">{t('expenses.summary.expenseCount', { count: expenseCount })}</p>
        </div>

        {budget !== null && remaining !== null ? (
          <>
            <div>
              <p className="text-sm font-medium text-gray-500">{t('expenses.summary.budget')}</p>
              <p className="mt-1 text-2xl font-semibold text-gray-900">{money(budget)}</p>
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500">
                {overBudget ? t('expenses.summary.overBudget') : t('expenses.summary.remaining')}
              </p>
              <p className={`mt-1 text-2xl font-semibold ${overBudget ? 'text-red-600' : 'text-emerald-600'}`}>
                {money(Math.abs(remaining))}
              </p>
            </div>
          </>
        ) : (
          <div className="sm:col-span-2 sm:self-center">
            <p className="text-sm text-gray-500">{t('expenses.summary.noBudget')}</p>
          </div>
        )}
      </div>

      {budget !== null && usagePercent !== null && (
        <div className="mt-5">
          <div
            className="h-2.5 w-full overflow-hidden rounded-full bg-gray-100"
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.min(usagePercent, 100)}
            aria-label={t('expenses.summary.usage', { percent: usagePercent })}
          >
            <div
              className={`h-full rounded-full ${usageColor(usagePercent)}`}
              style={{ width: `${Math.min(usagePercent, 100)}%` }}
            />
          </div>
          <p className={`mt-2 text-xs font-medium ${overBudget ? 'text-red-600' : 'text-gray-500'}`}>
            {t('expenses.summary.usage', { percent: usagePercent })}
          </p>
        </div>
      )}
    </section>
  );
}
