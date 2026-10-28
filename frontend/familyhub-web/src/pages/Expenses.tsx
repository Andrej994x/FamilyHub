import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { useFamily } from '../hooks/useFamily';
import { expenseService } from '../services/expenseService';
import { familyService } from '../services/familyService';
import { getApiErrorMessage } from '../utils/apiError';
import { categoryLabel } from '../utils/expenseCategories';
import { formatMoney } from '../utils/money';
import { currentMonth, type YearMonth } from '../utils/month';
import { canEditExpense } from '../utils/roles';
import { Card } from '../components/dashboard/Card';
import { EmptyState } from '../components/dashboard/EmptyState';
import { BudgetOverview } from '../components/expenses/BudgetOverview';
import { CategoryBreakdown } from '../components/expenses/CategoryBreakdown';
import { ExpenseDialog } from '../components/expenses/ExpenseDialog';
import { ExpenseList } from '../components/expenses/ExpenseList';
import { MonthSwitcher } from '../components/expenses/MonthSwitcher';
import type {
  ExpenseCategoryResponse,
  ExpenseResponse,
  ExpenseSummaryResponse,
  FamilyMemberResponse,
} from '../types';

type Tab = 'overview' | 'history';

const selectClass =
  'rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

const tabClass = (active: boolean) =>
  `shrink-0 rounded-full px-3.5 py-1.5 text-sm font-medium transition-colors ${
    active ? 'bg-brand-600 text-white' : 'bg-white text-gray-600 ring-1 ring-gray-200 hover:bg-gray-50'
  }`;

export default function Expenses() {
  const { t, i18n } = useTranslation();
  const { family, role } = useFamily();
  const { user } = useAuth();
  const familyId = family?.id ?? '';
  const [searchParams, setSearchParams] = useSearchParams();

  const thisMonth = useMemo(() => currentMonth(), []);
  const [month, setMonth] = useState<YearMonth>(thisMonth);
  const [tab, setTab] = useState<Tab>('overview');

  const [summary, setSummary] = useState<ExpenseSummaryResponse | null>(null);
  const [history, setHistory] = useState<ExpenseResponse[]>([]);
  const [categories, setCategories] = useState<ExpenseCategoryResponse[]>([]);
  const [members, setMembers] = useState<FamilyMemberResponse[]>([]);
  const [summaryLoading, setSummaryLoading] = useState(true);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyFailed, setHistoryFailed] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Bumped after a save or delete to reload the summary and history.
  const [version, setVersion] = useState(0);

  // History filters (the month comes from the switcher). `undefined` means "all".
  const [categoryFilter, setCategoryFilter] = useState<string | undefined>(undefined);
  const [payerFilter, setPayerFilter] = useState<string | undefined>(undefined);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<ExpenseResponse | null>(null);

  const reload = useCallback(() => setVersion((v) => v + 1), []);

  // Categories (all, so archived ones still have labels) and members, once per family.
  useEffect(() => {
    if (!familyId) {
      return;
    }
    expenseService
      .categories(familyId, true)
      .then(setCategories)
      .catch((err) => setError(getApiErrorMessage(err, t('expenses.errors.loadFailed'))));
    familyService
      .getMembers(familyId)
      .then(setMembers)
      .catch(() => setMembers([]));
  }, [familyId, t]);

  useEffect(() => {
    if (!familyId) {
      return;
    }
    let cancelled = false;
    setSummaryLoading(true);
    setError(null);
    expenseService
      .summary(familyId, month.year, month.month)
      .then((data) => !cancelled && setSummary(data))
      .catch((err) => {
        if (!cancelled) {
          // Never leave another (or an outdated) month's numbers on screen.
          setSummary(null);
          setError(getApiErrorMessage(err, t('expenses.errors.loadFailed')));
        }
      })
      .finally(() => !cancelled && setSummaryLoading(false));
    return () => {
      cancelled = true;
    };
  }, [familyId, month, version, t]);

  useEffect(() => {
    if (!familyId || tab !== 'history') {
      return;
    }
    let cancelled = false;
    setHistoryLoading(true);
    setHistoryFailed(false);
    setError(null);
    expenseService
      .list(familyId, { year: month.year, month: month.month, categoryId: categoryFilter, paidByUserId: payerFilter })
      .then((data) => !cancelled && setHistory(data))
      .catch((err) => {
        if (!cancelled) {
          // Show only the error: not the previous rows, and not a misleading "no expenses".
          setHistory([]);
          setHistoryFailed(true);
          setError(getApiErrorMessage(err, t('expenses.errors.loadFailed')));
        }
      })
      .finally(() => !cancelled && setHistoryLoading(false));
    return () => {
      cancelled = true;
    };
  }, [familyId, tab, month, categoryFilter, payerFilter, version, t]);

  const openCreate = useCallback(() => {
    setEditing(null);
    setDialogOpen(true);
  }, []);

  // The dashboard's "Add expense" quick action links here with ?add=1.
  useEffect(() => {
    if (searchParams.get('add') === '1') {
      openCreate();
      setSearchParams({}, { replace: true });
    }
  }, [searchParams, setSearchParams, openCreate]);

  const openEdit = (expense: ExpenseResponse) => {
    setEditing(expense);
    setDialogOpen(true);
  };

  const handleDelete = async (expense: ExpenseResponse) => {
    if (!window.confirm(t('expenses.deleteConfirm'))) {
      return;
    }
    try {
      await expenseService.remove(familyId, expense.id);
      reload();
    } catch (err) {
      setError(getApiErrorMessage(err, t('expenses.errors.deleteFailed')));
    }
  };

  const canEdit = useCallback(
    (expense: ExpenseResponse) => canEditExpense(role, expense.createdByUserId, user?.id),
    [role, user],
  );

  const sortOrders = useMemo(
    () => Object.fromEntries(categories.map((c) => [c.id, c.sortOrder])),
    [categories],
  );

  // Only the selected month's summary is shown, so a month switch never displays another month's numbers.
  const monthSummary =
    summary && summary.year === month.year && summary.month === month.month ? summary : null;
  const hasFilters = categoryFilter !== undefined || payerFilter !== undefined;
  const historyTotal = history.reduce((sum, e) => sum + e.amount, 0);

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-5xl">
      <header className="mb-6 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('nav.expenses')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('pages.expenses.subtitle')}</p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
        >
          {t('expenses.add')}
        </button>
      </header>

      <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <MonthSwitcher value={month} onChange={setMonth} max={thisMonth} />
        <div className="no-scrollbar flex gap-2 overflow-x-auto" role="tablist">
          <button type="button" role="tab" aria-selected={tab === 'overview'} onClick={() => setTab('overview')} className={tabClass(tab === 'overview')}>
            {t('expenses.tabs.overview')}
          </button>
          <button type="button" role="tab" aria-selected={tab === 'history'} onClick={() => setTab('history')} className={tabClass(tab === 'history')}>
            {t('expenses.tabs.history')}
          </button>
        </div>
      </div>

      {error && <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

      {tab === 'overview' ? (
        summaryLoading && !monthSummary ? (
          <p className="text-sm text-gray-400">{t('common.loading')}</p>
        ) : (
          monthSummary && (
            <div className="space-y-5">
              <BudgetOverview summary={monthSummary} />
              <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
                <CategoryBreakdown categories={monthSummary.categories} sortOrders={sortOrders} />
                <Card
                  title={t('expenses.recent.title')}
                  action={
                    monthSummary.expenseCount > 0 && (
                      <button
                        type="button"
                        onClick={() => setTab('history')}
                        className="text-xs font-medium text-brand-600 hover:underline"
                      >
                        {t('expenses.recent.viewAll')}
                      </button>
                    )
                  }
                >
                  {monthSummary.recentExpenses.length === 0 ? (
                    <EmptyState message={t('expenses.recent.empty')} />
                  ) : (
                    <ExpenseList expenses={monthSummary.recentExpenses} canEdit={canEdit} onEdit={openEdit} onDelete={handleDelete} />
                  )}
                </Card>
              </div>
            </div>
          )
        )
      ) : (
        <>
          <div className="mb-4 flex flex-wrap items-center gap-3">
            <select
              aria-label={t('expenses.filters.category')}
              value={categoryFilter ?? ''}
              onChange={(e) => setCategoryFilter(e.target.value === '' ? undefined : e.target.value)}
              className={selectClass}
            >
              <option value="">{t('expenses.filters.allCategories')}</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.isArchived
                    ? t('expenses.filters.archivedOption', { name: categoryLabel(category.name, category.defaultCategory, t) })
                    : categoryLabel(category.name, category.defaultCategory, t)}
                </option>
              ))}
            </select>

            <select
              aria-label={t('expenses.filters.payer')}
              value={payerFilter ?? ''}
              onChange={(e) => setPayerFilter(e.target.value === '' ? undefined : e.target.value)}
              className={selectClass}
            >
              <option value="">{t('expenses.filters.allPayers')}</option>
              {members.map((member) => (
                <option key={member.userId} value={member.userId}>
                  {member.firstName} {member.lastName}
                </option>
              ))}
            </select>

            {hasFilters && (
              <button
                type="button"
                onClick={() => {
                  setCategoryFilter(undefined);
                  setPayerFilter(undefined);
                }}
                className="text-sm font-medium text-brand-600 hover:underline"
              >
                {t('expenses.filters.clear')}
              </button>
            )}
          </div>

          {historyFailed ? null : historyLoading ? (
            <p className="text-sm text-gray-400">{t('common.loading')}</p>
          ) : history.length === 0 ? (
            <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
              {hasFilters ? t('expenses.emptyFiltered') : t('expenses.empty')}
            </div>
          ) : (
            <>
              <p className="mb-3 text-sm text-gray-500">
                {t('expenses.history.total', {
                  count: history.length,
                  amount: formatMoney(historyTotal, i18n.language),
                })}
              </p>
              <ExpenseList expenses={history} canEdit={canEdit} onEdit={openEdit} onDelete={handleDelete} />
            </>
          )}
        </>
      )}

      <ExpenseDialog
        open={dialogOpen}
        familyId={familyId}
        expense={editing}
        categories={categories}
        members={members}
        currentUserId={user?.id ?? ''}
        onClose={() => setDialogOpen(false)}
        onSaved={reload}
      />
    </div>
  );
}
