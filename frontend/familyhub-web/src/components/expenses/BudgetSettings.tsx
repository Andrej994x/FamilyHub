import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { expenseService } from '../../services/expenseService';
import { getApiErrorMessage } from '../../utils/apiError';
import { categoryLabel } from '../../utils/expenseCategories';
import { formatMoney, MAX_AMOUNT, parseAmount, toAmountInput } from '../../utils/money';
import { compareMonths, formatMonthYear, type YearMonth } from '../../utils/month';
import type { ExpenseBudgetResponse, ExpenseCategoryResponse } from '../../types';
import { Card } from '../dashboard/Card';
import { MonthSwitcher } from './MonthSwitcher';

interface BudgetSettingsProps {
  familyId: string;
  /** All of the family's categories (archived ones included). */
  categories: ExpenseCategoryResponse[];
  /** Owners and parents edit; other members see the budgets read-only. */
  canManage: boolean;
  initialMonth: YearMonth;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

const OVERALL = 'overall';

/** The budgets as form values (as the user would type them), keyed by OVERALL or category id. */
function toInputs(data: ExpenseBudgetResponse, language: string): Record<string, string> {
  const values: Record<string, string> = {
    [OVERALL]: data.overallAmount === null ? '' : toAmountInput(data.overallAmount, language),
  };
  for (const budget of data.categories) {
    values[budget.categoryId] = toAmountInput(budget.amount, language);
  }
  return values;
}

/**
 * The overall and per-category monthly budgets for a selected month. Saving sends the complete set,
 * which applies from that month until it is changed; earlier months keep their budgets.
 */
export function BudgetSettings({ familyId, categories, canManage, initialMonth }: BudgetSettingsProps) {
  const { t, i18n } = useTranslation();
  const money = (amount: number) => formatMoney(amount, i18n.language);
  const label = (c: ExpenseCategoryResponse) => categoryLabel(c.name, c.defaultCategory, t);

  const [month, setMonth] = useState<YearMonth>(initialMonth);
  // The month on screen, for discarding a save that finishes after the user switched months.
  const shownMonth = useRef(month);
  useEffect(() => {
    shownMonth.current = month;
  }, [month]);
  const [budgets, setBudgets] = useState<ExpenseBudgetResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  // Bumped to reload the shown month, e.g. after an earlier month's save that carries into it.
  const [reloadKey, setReloadKey] = useState(0);
  // Form values keyed by OVERALL or a category id ('' means no budget): the loaded values, and the
  // user's unsaved edits on top. A reload refreshes only untouched fields, so edits aren't lost.
  const [serverInputs, setServerInputs] = useState<Record<string, string>>({});
  const [edits, setEdits] = useState<Record<string, string>>({});
  const inputs = { ...serverInputs, ...edits };
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const activeCategories = useMemo(
    () => categories.filter((c) => !c.isArchived).sort((a, b) => a.sortOrder - b.sortOrder),
    [categories],
  );

  useEffect(() => {
    if (!familyId) {
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    setSaved(false);
    expenseService
      .budgets(familyId, month.year, month.month)
      .then((data) => {
        if (!cancelled) {
          setBudgets(data);
          setServerInputs(toInputs(data, i18n.language));
          setFieldErrors({});
        }
      })
      .catch((err) => {
        if (!cancelled) {
          // Never leave another month's budgets on screen.
          setBudgets(null);
          setError(getApiErrorMessage(err, t('expenses.settings.errors.loadFailed')));
        }
      })
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
    // `categories` changes after every category add, rename, archive, restore or delete: reload, since
    // a deleted category's budgets are gone on the server and must not be re-sent on the next save.
  }, [familyId, month, categories, reloadKey, t, i18n.language]);

  // Only the selected month's budgets are shown.
  const current = budgets && budgets.year === month.year && budgets.month === month.month ? budgets : null;
  const budgetFor = (categoryId: string) => current?.categories.find((b) => b.categoryId === categoryId)?.amount ?? null;
  const archivedWithBudget = categories.filter((c) => c.isArchived && budgetFor(c.id) !== null);

  const categoryTotal = activeCategories.reduce((sum, c) => sum + (parseAmount(inputs[c.id] ?? '') ?? 0), 0);

  const setInput = (key: string, value: string) => {
    setEdits((previous) => ({ ...previous, [key]: value }));
    setSaved(false);
  };

  /** A budget field: '' is no budget, otherwise a valid MKD amount. Returns an error message or the value. */
  const readField = (key: string): { value: number | null; error?: string } => {
    const text = (inputs[key] ?? '').trim();
    if (text === '') {
      return { value: null };
    }
    const amount = parseAmount(text);
    if (amount === null || amount <= 0) {
      return { value: null, error: t('expenses.validation.amountInvalid') };
    }
    if (amount > MAX_AMOUNT) {
      return { value: null, error: t('expenses.validation.amountTooLarge') };
    }
    return { value: amount };
  };

  const handleSave = async (event: React.FormEvent) => {
    event.preventDefault();
    const errors: Record<string, string> = {};
    const overall = readField(OVERALL);
    if (overall.error) errors[OVERALL] = overall.error;

    const categoryBudgets: { categoryId: string; amount: number | null }[] = [];
    for (const category of activeCategories) {
      const field = readField(category.id);
      if (field.error) {
        errors[category.id] = field.error;
      } else if (field.value !== null) {
        categoryBudgets.push({ categoryId: category.id, amount: field.value });
      }
    }
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) {
      return;
    }

    // Keep, unchanged, budgets of categories this page doesn't know about (e.g. added by someone else
    // after it loaded): the user never saw them, so the complete set must not remove them.
    const known = new Set(categories.map((c) => c.id));
    for (const budget of current?.categories ?? []) {
      if (!known.has(budget.categoryId)) {
        categoryBudgets.push({ categoryId: budget.categoryId, amount: budget.amount });
      }
    }

    const savedMonth = month;
    const isStillShown = () =>
      shownMonth.current.year === savedMonth.year && shownMonth.current.month === savedMonth.month;
    setSaving(true);
    setError(null);
    try {
      // The complete set: categories left out (empty or archived) lose their budget from this month on.
      const data = await expenseService.setBudgets(familyId, {
        year: savedMonth.year,
        month: savedMonth.month,
        overallAmount: overall.value,
        categories: categoryBudgets,
      });
      // The save went through, but another month is on screen now: don't overwrite it. If that month
      // is later, the saved budgets may carry into it, so reload it instead of showing outdated values.
      if (isStillShown()) {
        setBudgets(data);
        setServerInputs(toInputs(data, i18n.language));
        setEdits({});
        setSaved(true);
      } else if (compareMonths(savedMonth, shownMonth.current) < 0) {
        setReloadKey((key) => key + 1);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, t('expenses.settings.errors.saveFailed')));
    } finally {
      setSaving(false);
    }
  };

  const fieldError = (key: string) =>
    fieldErrors[key] && (
      <p id={`budget-${key}-error`} className="mt-1 text-xs text-red-600">
        {fieldErrors[key]}
      </p>
    );

  return (
    <Card title={t('expenses.settings.budgets.title')}>
      <div className="mb-4">
        <MonthSwitcher
          value={month}
          onChange={(next) => {
            // Edits belong to the month they were typed for.
            setEdits({});
            setMonth(next);
          }}
        />
      </div>

      {error && <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

      {loading && !current ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : !current ? null : canManage ? (
        <form onSubmit={handleSave} noValidate className="space-y-5">
          <p className="text-xs text-gray-500">
            {t('expenses.settings.budgets.appliesFrom', { month: formatMonthYear(month, i18n.language) })}
          </p>

          {/* Locked while saving, so nothing typed now can be overwritten by the save's response. */}
          <fieldset disabled={saving} className="space-y-5">
            <div>
              <label htmlFor="budget-overall" className="mb-1 block text-sm font-medium text-gray-700">
                {t('expenses.settings.budgets.overall')}
              </label>
              <input
                id="budget-overall"
                type="text"
                inputMode="decimal"
                autoComplete="off"
                placeholder={t('expenses.settings.budgets.none')}
                value={inputs[OVERALL] ?? ''}
                onChange={(e) => setInput(OVERALL, e.target.value)}
                aria-invalid={Boolean(fieldErrors[OVERALL])}
                aria-describedby={fieldErrors[OVERALL] ? `budget-${OVERALL}-error` : undefined}
                className={inputClass}
              />
              {fieldError(OVERALL)}
            </div>

            <fieldset>
              <legend className="text-sm font-medium text-gray-700">{t('expenses.settings.budgets.categoryBudgets')}</legend>
              <p className="mt-0.5 text-xs text-gray-400">{t('expenses.settings.budgets.emptyHint')}</p>
              <div className="mt-3 space-y-3">
                {activeCategories.map((category) => (
                  <div key={category.id} className="grid grid-cols-[1fr_9rem] items-start gap-3 sm:grid-cols-[1fr_12rem]">
                    <label htmlFor={`budget-${category.id}`} className="truncate pt-2 text-sm text-gray-800">
                      {label(category)}
                    </label>
                    <div>
                      <input
                        id={`budget-${category.id}`}
                        type="text"
                        inputMode="decimal"
                        autoComplete="off"
                        placeholder={t('expenses.settings.budgets.none')}
                        value={inputs[category.id] ?? ''}
                        onChange={(e) => setInput(category.id, e.target.value)}
                        aria-invalid={Boolean(fieldErrors[category.id])}
                        aria-describedby={fieldErrors[category.id] ? `budget-${category.id}-error` : undefined}
                        className={inputClass}
                      />
                      {fieldError(category.id)}
                    </div>
                  </div>
                ))}
              </div>
              {categoryTotal > 0 && (
                <p className="mt-3 text-xs text-gray-500">
                  {t('expenses.settings.budgets.categoryTotal', { amount: money(categoryTotal) })}
                </p>
              )}
              {archivedWithBudget.length > 0 && (
                <p className="mt-2 text-xs text-amber-700">{t('expenses.settings.budgets.archivedRemoved')}</p>
              )}
            </fieldset>
          </fieldset>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
            <button
              type="submit"
              disabled={saving || loading}
              className="rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
            >
              {saving ? t('common.saving') : t('expenses.settings.budgets.save')}
            </button>
            {saved && (
              <p className="text-sm text-emerald-700" role="status">
                {t('expenses.settings.budgets.saved')}
              </p>
            )}
          </div>
        </form>
      ) : (
        <dl className="space-y-3 text-sm">
          <div className="flex items-baseline justify-between gap-3">
            <dt className="font-medium text-gray-700">{t('expenses.settings.budgets.overall')}</dt>
            <dd className="tabular-nums text-gray-900">
              {current.overallAmount === null ? t('expenses.settings.budgets.none') : money(current.overallAmount)}
            </dd>
          </div>
          {categories
            .filter((c) => budgetFor(c.id) !== null)
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map((category) => (
              <div key={category.id} className="flex items-baseline justify-between gap-3">
                <dt className="truncate text-gray-600">{label(category)}</dt>
                <dd className="shrink-0 tabular-nums text-gray-900">{money(budgetFor(category.id)!)}</dd>
              </div>
            ))}
          {current.overallAmount === null && current.categories.length === 0 && (
            <p className="text-gray-500">{t('expenses.settings.budgets.noneSet')}</p>
          )}
        </dl>
      )}
    </Card>
  );
}
