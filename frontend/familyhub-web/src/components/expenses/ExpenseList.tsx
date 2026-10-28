import { useTranslation } from 'react-i18next';
import type { ExpenseResponse } from '../../types';
import { formatMoney } from '../../utils/money';
import { formatDateOnly } from '../../utils/month';
import { categoryLabel } from '../../utils/expenseCategories';

interface ExpenseListProps {
  expenses: ExpenseResponse[];
  /** Whether the current user may edit/delete this expense (cosmetic; the API enforces it). */
  canEdit: (expense: ExpenseResponse) => boolean;
  onEdit: (expense: ExpenseResponse) => void;
  onDelete: (expense: ExpenseResponse) => void;
}

/** Expenses as cards on phones and a table from the `sm` breakpoint up. */
export function ExpenseList({ expenses, canEdit, onEdit, onDelete }: ExpenseListProps) {
  const { t, i18n } = useTranslation();
  const money = (amount: number) => formatMoney(amount, i18n.language);
  const label = (expense: ExpenseResponse) => categoryLabel(expense.categoryName, expense.categoryDefault, t);
  const payer = (expense: ExpenseResponse) => expense.paidByName || t('expenses.formerMember');

  const actions = (expense: ExpenseResponse) =>
    canEdit(expense) && (
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => onEdit(expense)}
          className="text-xs font-medium text-brand-600 hover:underline"
          aria-label={`${t('common.edit')}: ${label(expense)} ${money(expense.amount)}`}
        >
          {t('common.edit')}
        </button>
        <button
          type="button"
          onClick={() => onDelete(expense)}
          className="text-xs font-medium text-red-600 hover:underline"
          aria-label={`${t('common.delete')}: ${label(expense)} ${money(expense.amount)}`}
        >
          {t('common.delete')}
        </button>
      </div>
    );

  return (
    <>
      {/* Mobile cards */}
      <ul className="space-y-3 sm:hidden">
        {expenses.map((expense) => (
          <li key={expense.id} className="rounded-xl border border-gray-200 bg-white p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="truncate font-medium text-gray-900">{label(expense)}</p>
                {expense.description && (
                  <p className="mt-0.5 line-clamp-2 text-sm text-gray-500">{expense.description}</p>
                )}
              </div>
              <p className="shrink-0 font-semibold tabular-nums text-gray-900">{money(expense.amount)}</p>
            </div>
            <div className="mt-3 flex items-center justify-between gap-3">
              <p className="text-xs text-gray-500">
                {formatDateOnly(expense.date, i18n.language)} · {t('expenses.paidBy', { name: payer(expense) })}
              </p>
              {actions(expense)}
            </div>
          </li>
        ))}
      </ul>

      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-xl border border-gray-200 bg-white sm:block">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-gray-100 bg-gray-50 text-xs font-medium uppercase tracking-wide text-gray-500">
            <tr>
              <th className="px-4 py-3">{t('expenses.table.date')}</th>
              <th className="px-4 py-3">{t('expenses.table.category')}</th>
              <th className="px-4 py-3">{t('expenses.table.paidBy')}</th>
              <th className="px-4 py-3 text-right">{t('expenses.table.amount')}</th>
              <th className="px-4 py-3 text-right">
                <span className="sr-only">{t('expenses.table.actions')}</span>
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {expenses.map((expense) => (
              <tr key={expense.id} className="align-top">
                <td className="whitespace-nowrap px-4 py-3 text-gray-600">{formatDateOnly(expense.date, i18n.language)}</td>
                <td className="px-4 py-3">
                  <p className="font-medium text-gray-900">{label(expense)}</p>
                  {expense.description && (
                    <p className="mt-0.5 line-clamp-1 text-xs text-gray-500">{expense.description}</p>
                  )}
                </td>
                <td className="px-4 py-3 text-gray-600">{payer(expense)}</td>
                <td className="whitespace-nowrap px-4 py-3 text-right font-semibold tabular-nums text-gray-900">
                  {money(expense.amount)}
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end">{actions(expense)}</div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
