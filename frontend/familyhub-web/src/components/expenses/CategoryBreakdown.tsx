import { useTranslation } from 'react-i18next';
import type { CategorySpendingResponse } from '../../types';
import { formatMoney } from '../../utils/money';
import { categoryColor, categoryLabel } from '../../utils/expenseCategories';
import { Card } from '../dashboard/Card';
import { EmptyState } from '../dashboard/EmptyState';

interface CategoryBreakdownProps {
  categories: CategorySpendingResponse[];
  /** Sort order per category id, for stable colours. */
  sortOrders: Record<string, number>;
}

/**
 * Spending per category as horizontal bars. With a category budget the bar shows the share of
 * that budget; otherwise it is relative to the largest category this month.
 */
export function CategoryBreakdown({ categories, sortOrders }: CategoryBreakdownProps) {
  const { t, i18n } = useTranslation();
  const money = (amount: number) => formatMoney(amount, i18n.language);
  const largest = Math.max(0, ...categories.map((c) => c.spent));

  return (
    <Card title={t('expenses.byCategory.title')}>
      {categories.length === 0 ? (
        <EmptyState message={t('expenses.byCategory.empty')} />
      ) : (
        <ul className="space-y-4">
          {categories.map((category) => {
            const overBudget = category.remaining !== null && category.remaining < 0;
            const width =
              category.usagePercent !== null
                ? Math.min(category.usagePercent, 100)
                : largest > 0
                  ? (category.spent / largest) * 100
                  : 0;
            const label = categoryLabel(category.name, category.defaultCategory, t);

            return (
              <li key={category.categoryId}>
                <div className="flex items-baseline justify-between gap-3 text-sm">
                  <span className="min-w-0 truncate font-medium text-gray-800">
                    {label}
                    {category.isArchived && (
                      <span className="ml-2 text-xs font-normal text-gray-400">{t('expenses.archived')}</span>
                    )}
                  </span>
                  <span className={`shrink-0 tabular-nums ${overBudget ? 'font-semibold text-red-600' : 'text-gray-700'}`}>
                    {money(category.spent)}
                    {category.budget !== null && (
                      <span className="ml-1 font-normal text-gray-400">
                        {t('expenses.byCategory.ofBudget', { budget: money(category.budget) })}
                      </span>
                    )}
                  </span>
                </div>
                <div className="mt-1.5 h-2 w-full overflow-hidden rounded-full bg-gray-100" aria-hidden="true">
                  <div
                    className={`h-full rounded-full ${overBudget ? 'bg-red-500' : categoryColor(sortOrders[category.categoryId] ?? 0)}`}
                    style={{ width: `${width}%` }}
                  />
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </Card>
  );
}
