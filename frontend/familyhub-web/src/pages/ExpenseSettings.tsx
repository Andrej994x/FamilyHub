import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useFamily } from '../hooks/useFamily';
import { expenseService } from '../services/expenseService';
import { getApiErrorMessage } from '../utils/apiError';
import { currentMonth, type YearMonth } from '../utils/month';
import { canManageFamily } from '../utils/roles';
import { BudgetSettings } from '../components/expenses/BudgetSettings';
import { CategorySettings } from '../components/expenses/CategorySettings';
import type { ExpenseCategoryResponse } from '../types';

/** The month from `?year=&month=` (as linked from the Expenses page), else the current month. */
function monthFromQuery(params: URLSearchParams): YearMonth {
  const year = Number(params.get('year'));
  const month = Number(params.get('month'));
  return Number.isInteger(year) && year >= 2000 && year <= 2100 && Number.isInteger(month) && month >= 1 && month <= 12
    ? { year, month }
    : currentMonth();
}

/** Budgets and categories. Owners and parents manage them; other members see them read-only. */
export default function ExpenseSettings() {
  const { t } = useTranslation();
  const { family, role } = useFamily();
  const familyId = family?.id ?? '';
  const [searchParams] = useSearchParams();
  const canManage = canManageFamily(role);
  // Read once: the budget section keeps its own month after that.
  const [initialMonth] = useState(() => monthFromQuery(searchParams));

  // Null until the categories have loaded once. The budget form needs the full list: saving sends the
  // complete set, so a missing list would remove category budgets.
  const [categories, setCategories] = useState<ExpenseCategoryResponse[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadCategories = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setLoading(true);
    try {
      setCategories(await expenseService.categories(familyId, true));
      setError(null);
    } catch (err) {
      // A failed reload keeps the last complete list; a failed first load leaves it null.
      setError(getApiErrorMessage(err, t('expenses.settings.errors.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [familyId, t]);

  useEffect(() => {
    void loadCategories();
  }, [loadCategories]);

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-3xl">
      <Link to="/expenses" className="mb-3 inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:underline">
        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor" aria-hidden="true">
          <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
        </svg>
        {t('expenses.settings.back')}
      </Link>

      <header className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">{t('expenses.settings.title')}</h1>
        <p className="mt-1 text-sm text-gray-500">{t('expenses.settings.subtitle')}</p>
      </header>

      {!canManage && (
        <div className="mb-4 rounded-lg bg-sky-50 px-3 py-2 text-sm text-sky-800">{t('expenses.settings.readOnly')}</div>
      )}

      {error && <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

      {categories === null ? (
        loading ? (
          <p className="text-sm text-gray-400">{t('common.loading')}</p>
        ) : (
          <button
            type="button"
            onClick={() => void loadCategories()}
            className="rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {t('common.retry')}
          </button>
        )
      ) : (
        <div className="space-y-5">
          <BudgetSettings familyId={familyId} categories={categories} canManage={canManage} initialMonth={initialMonth} />
          <CategorySettings familyId={familyId} categories={categories} canManage={canManage} onChanged={loadCategories} />
        </div>
      )}
    </div>
  );
}
