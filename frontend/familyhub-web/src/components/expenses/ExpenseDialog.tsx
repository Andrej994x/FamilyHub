import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from '../Modal';
import { expenseService } from '../../services/expenseService';
import { getApiErrorMessage } from '../../utils/apiError';
import { categoryLabel } from '../../utils/expenseCategories';
import { MAX_AMOUNT, parseAmount, toAmountInput } from '../../utils/money';
import { todayDateOnly } from '../../utils/month';
import type { ExpenseCategoryResponse, ExpenseResponse, FamilyMemberResponse } from '../../types';

interface ExpenseDialogProps {
  open: boolean;
  familyId: string;
  expense: ExpenseResponse | null;
  /** All of the family's categories; archived ones are offered only to keep an existing value. */
  categories: ExpenseCategoryResponse[];
  members: FamilyMemberResponse[];
  currentUserId: string;
  onClose: () => void;
  onSaved: () => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

export function ExpenseDialog({
  open,
  familyId,
  expense,
  categories,
  members,
  currentUserId,
  onClose,
  onSaved,
}: ExpenseDialogProps) {
  const { t, i18n } = useTranslation();
  const [amount, setAmount] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [date, setDate] = useState('');
  const [paidByUserId, setPaidByUserId] = useState('');
  const [description, setDescription] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const today = todayDateOnly();

  // Active categories, plus the expense's own category when it has been archived since.
  const categoryOptions = useMemo(
    () => categories.filter((c) => !c.isArchived || c.id === expense?.categoryId),
    [categories, expense],
  );

  // Current members, plus the expense's payer when they have left the family.
  const payerOptions = useMemo(() => {
    const options = members.map((m) => ({ userId: m.userId, name: `${m.firstName} ${m.lastName}` }));
    if (expense && !options.some((o) => o.userId === expense.paidByUserId)) {
      options.push({ userId: expense.paidByUserId, name: expense.paidByName || t('expenses.formerMember') });
    }
    return options;
  }, [members, expense, t]);

  // Sync form fields whenever the dialog opens or the target expense changes.
  useEffect(() => {
    if (!open) {
      return;
    }
    setAmount(expense ? toAmountInput(expense.amount, i18n.language) : '');
    setCategoryId(expense?.categoryId ?? '');
    setDate(expense?.date ?? todayDateOnly());
    setPaidByUserId(expense?.paidByUserId ?? currentUserId);
    setDescription(expense?.description ?? '');
    setFieldErrors({});
    setError(null);
  }, [open, expense, currentUserId, i18n.language]);

  const validate = (parsedAmount: number | null): Record<string, string> => {
    const errors: Record<string, string> = {};
    if (parsedAmount === null || parsedAmount <= 0) {
      errors.amount = t('expenses.validation.amountInvalid');
    } else if (parsedAmount > MAX_AMOUNT) {
      errors.amount = t('expenses.validation.amountTooLarge');
    }
    if (!categoryId) {
      errors.categoryId = t('expenses.validation.required');
    }
    if (!date) {
      errors.date = t('expenses.validation.required');
    } else if (date > today) {
      errors.date = t('expenses.validation.dateInFuture');
    }
    if (!paidByUserId) {
      errors.paidByUserId = t('expenses.validation.required');
    }
    return errors;
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const parsedAmount = parseAmount(amount);
    const errors = validate(parsedAmount);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0 || parsedAmount === null) {
      return;
    }

    setSubmitting(true);
    setError(null);
    const payload = {
      amount: parsedAmount,
      categoryId,
      date,
      paidByUserId,
      description: description.trim() === '' ? null : description.trim(),
    };

    try {
      if (expense) {
        await expenseService.update(familyId, expense.id, payload);
      } else {
        await expenseService.create(familyId, payload);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('expenses.errors.saveFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  const fieldError = (name: string) =>
    fieldErrors[name] && (
      <p id={`expense-${name}-error`} className="mt-1 text-xs text-red-600">
        {fieldErrors[name]}
      </p>
    );

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={expense ? t('expenses.dialog.editTitle') : t('expenses.dialog.addTitle')}
    >
      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        <div>
          <label htmlFor="expense-amount" className="mb-1 block text-sm font-medium text-gray-700">
            {t('expenses.dialog.amount')}
          </label>
          <input
            id="expense-amount"
            type="text"
            inputMode="decimal"
            autoComplete="off"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            aria-invalid={Boolean(fieldErrors.amount)}
            aria-describedby={fieldErrors.amount ? 'expense-amount-error' : undefined}
            className={inputClass}
          />
          {fieldError('amount')}
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label htmlFor="expense-category" className="mb-1 block text-sm font-medium text-gray-700">
              {t('expenses.dialog.category')}
            </label>
            <select
              id="expense-category"
              value={categoryId}
              onChange={(event) => setCategoryId(event.target.value)}
              aria-invalid={Boolean(fieldErrors.categoryId)}
              aria-describedby={fieldErrors.categoryId ? 'expense-categoryId-error' : undefined}
              className={inputClass}
            >
              <option value="">{t('expenses.dialog.chooseCategory')}</option>
              {categoryOptions.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.isArchived
                    ? t('expenses.filters.archivedOption', { name: categoryLabel(category.name, category.defaultCategory, t) })
                    : categoryLabel(category.name, category.defaultCategory, t)}
                </option>
              ))}
            </select>
            {fieldError('categoryId')}
          </div>

          <div>
            <label htmlFor="expense-date" className="mb-1 block text-sm font-medium text-gray-700">
              {t('expenses.dialog.date')}
            </label>
            <input
              id="expense-date"
              type="date"
              max={today}
              value={date}
              onChange={(event) => setDate(event.target.value)}
              aria-invalid={Boolean(fieldErrors.date)}
              aria-describedby={fieldErrors.date ? 'expense-date-error' : undefined}
              className={inputClass}
            />
            {fieldError('date')}
          </div>
        </div>

        <div>
          <label htmlFor="expense-payer" className="mb-1 block text-sm font-medium text-gray-700">
            {t('expenses.dialog.paidBy')}
          </label>
          <select
            id="expense-payer"
            value={paidByUserId}
            onChange={(event) => setPaidByUserId(event.target.value)}
            aria-invalid={Boolean(fieldErrors.paidByUserId)}
            aria-describedby={fieldErrors.paidByUserId ? 'expense-paidByUserId-error' : undefined}
            className={inputClass}
          >
            {payerOptions.map((option) => (
              <option key={option.userId} value={option.userId}>
                {option.userId === currentUserId ? `${option.name} (${t('common.you')})` : option.name}
              </option>
            ))}
          </select>
          {fieldError('paidByUserId')}
        </div>

        <div>
          <label htmlFor="expense-description" className="mb-1 block text-sm font-medium text-gray-700">
            {t('expenses.dialog.description')}{' '}
            <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
          </label>
          <textarea
            id="expense-description"
            rows={2}
            maxLength={500}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            className={inputClass}
          />
        </div>

        {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

        <div className="flex gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {t('common.cancel')}
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {submitting ? t('common.saving') : t('expenses.dialog.submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
