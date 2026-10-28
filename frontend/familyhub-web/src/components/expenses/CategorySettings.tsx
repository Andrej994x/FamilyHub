import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { expenseService } from '../../services/expenseService';
import { getApiErrorMessage } from '../../utils/apiError';
import { categoryLabel } from '../../utils/expenseCategories';
import type { ExpenseCategoryResponse } from '../../types';
import { Card } from '../dashboard/Card';
import { CategoryDialog } from './CategoryDialog';

interface CategorySettingsProps {
  familyId: string;
  /** All of the family's categories (archived ones included). */
  categories: ExpenseCategoryResponse[];
  /** Owners and parents manage categories; other members see them read-only. */
  canManage: boolean;
  /** Called after any change so the parent reloads the categories. */
  onChanged: () => void;
}

const actionClass = 'text-xs font-medium hover:underline disabled:cursor-not-allowed disabled:opacity-50';

/**
 * Active categories (rename, archive) and archived ones (restore, delete when unused). A category
 * used by expenses can only be archived, so the history keeps it; the API enforces this.
 */
export function CategorySettings({ familyId, categories, canManage, onChanged }: CategorySettingsProps) {
  const { t } = useTranslation();
  const label = (c: ExpenseCategoryResponse) => categoryLabel(c.name, c.defaultCategory, t);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [dialog, setDialog] = useState<{ mode: 'add' | 'rename'; category: ExpenseCategoryResponse | null } | null>(null);

  const byOrder = (a: ExpenseCategoryResponse, b: ExpenseCategoryResponse) => a.sortOrder - b.sortOrder;
  const active = categories.filter((c) => !c.isArchived).sort(byOrder);
  const archived = categories.filter((c) => c.isArchived).sort(byOrder);

  const run = async (action: () => Promise<unknown>, fallbackKey: string) => {
    setBusy(true);
    setError(null);
    try {
      await action();
      onChanged();
    } catch (err) {
      setError(getApiErrorMessage(err, t(fallbackKey)));
    } finally {
      setBusy(false);
    }
  };

  // Archive and restore keep the stored name, so a built-in category stays translated.
  const setArchived = (category: ExpenseCategoryResponse, isArchived: boolean) =>
    run(
      () => expenseService.updateCategory(familyId, category.id, { name: category.name, isArchived }),
      'expenses.settings.errors.categorySaveFailed',
    );

  const remove = (category: ExpenseCategoryResponse) => {
    if (!window.confirm(t('expenses.settings.categories.deleteConfirm'))) {
      return;
    }
    void run(() => expenseService.deleteCategory(familyId, category.id), 'expenses.settings.errors.categoryDeleteFailed');
  };

  const submitDialog = async (name: string) => {
    if (dialog?.mode === 'rename' && dialog.category) {
      await expenseService.updateCategory(familyId, dialog.category.id, { name, isArchived: dialog.category.isArchived });
    } else {
      await expenseService.createCategory(familyId, { name });
    }
    onChanged();
  };

  const row = (category: ExpenseCategoryResponse, actions: React.ReactNode) => (
    <li key={category.id} className="flex items-center justify-between gap-3 py-3">
      <span className="min-w-0 truncate text-sm font-medium text-gray-800">{label(category)}</span>
      {canManage && <div className="flex shrink-0 items-center gap-4">{actions}</div>}
    </li>
  );

  return (
    <Card
      title={t('expenses.settings.categories.title')}
      action={
        canManage && (
          <button
            type="button"
            onClick={() => setDialog({ mode: 'add', category: null })}
            className="rounded-lg bg-brand-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-brand-700"
          >
            {t('expenses.settings.categories.add')}
          </button>
        )
      }
    >
      {error && <div className="mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

      <ul className="divide-y divide-gray-100" aria-label={t('expenses.settings.categories.title')}>
        {active.map((category) =>
          row(
            category,
            <>
              <button
                type="button"
                disabled={busy}
                onClick={() => setDialog({ mode: 'rename', category })}
                className={`${actionClass} text-brand-600`}
                aria-label={`${t('expenses.settings.categories.rename')}: ${label(category)}`}
              >
                {t('expenses.settings.categories.rename')}
              </button>
              <button
                type="button"
                disabled={busy}
                onClick={() => void setArchived(category, true)}
                className={`${actionClass} text-gray-600`}
                aria-label={`${t('expenses.settings.categories.archive')}: ${label(category)}`}
              >
                {t('expenses.settings.categories.archive')}
              </button>
            </>,
          ),
        )}
      </ul>

      {archived.length > 0 && (
        <details className="mt-4 rounded-xl bg-gray-50 px-4 py-3">
          <summary className="cursor-pointer text-sm font-medium text-gray-700">
            {t('expenses.settings.categories.archivedTitle', { count: archived.length })}
          </summary>
          <p className="mt-2 text-xs text-gray-500">{t('expenses.settings.categories.archivedHint')}</p>
          <ul className="mt-1 divide-y divide-gray-200" aria-label={t('expenses.settings.categories.archivedTitle', { count: archived.length })}>
            {archived.map((category) =>
              row(
                category,
                <>
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => void setArchived(category, false)}
                    className={`${actionClass} text-brand-600`}
                    aria-label={`${t('expenses.settings.categories.restore')}: ${label(category)}`}
                  >
                    {t('expenses.settings.categories.restore')}
                  </button>
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => remove(category)}
                    className={`${actionClass} text-red-600`}
                    aria-label={`${t('common.delete')}: ${label(category)}`}
                  >
                    {t('common.delete')}
                  </button>
                </>,
              ),
            )}
          </ul>
        </details>
      )}

      <CategoryDialog
        open={dialog !== null}
        mode={dialog?.mode ?? 'add'}
        initialName={dialog?.category ? label(dialog.category) : ''}
        isBuiltIn={dialog?.category?.defaultCategory != null}
        onClose={() => setDialog(null)}
        onSubmit={submitDialog}
      />
    </Card>
  );
}
