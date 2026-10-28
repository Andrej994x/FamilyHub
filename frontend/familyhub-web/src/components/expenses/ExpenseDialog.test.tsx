import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ExpenseDialog } from './ExpenseDialog';
import { expenseService } from '../../services/expenseService';
import i18n from '../../locales/i18n';
import en from '../../locales/en/translation.json';
import mk from '../../locales/mk/translation.json';
import { todayDateOnly } from '../../utils/month';
import type { ExpenseCategoryResponse, ExpenseResponse, FamilyMemberResponse } from '../../types';

vi.mock('../../services/expenseService', () => ({
  expenseService: { create: vi.fn(), update: vi.fn() },
}));

const categories: ExpenseCategoryResponse[] = [
  { id: 'cat-food', name: 'Food & Groceries', defaultCategory: 0, isArchived: false, sortOrder: 0 },
  { id: 'cat-fuel', name: 'Fuel', defaultCategory: null, isArchived: false, sortOrder: 9 },
  { id: 'cat-old', name: 'Old hobby', defaultCategory: null, isArchived: true, sortOrder: 10 },
];

const members: FamilyMemberResponse[] = [
  { id: 'm1', userId: 'user-1', firstName: 'Ana', lastName: 'Petrova', email: 'ana@x', role: 0, joinedAt: '' },
  { id: 'm2', userId: 'user-2', firstName: 'Bojan', lastName: 'Petrov', email: 'bojan@x', role: 2, joinedAt: '' },
];

function expense(overrides: Partial<ExpenseResponse> = {}): ExpenseResponse {
  return {
    id: 'exp-1', familyId: 'family-1', amount: 1234.5, date: '2026-10-05', categoryId: 'cat-old',
    categoryName: 'Old hobby', categoryDefault: null, categoryIsArchived: true, paidByUserId: 'former-user',
    paidByName: 'Former Payer', description: 'Paint', createdByUserId: 'user-1', createdByName: 'Ana Petrova',
    createdAt: '2026-10-05T10:00:00Z', updatedAt: null, ...overrides,
  };
}

function renderDialog(existing: ExpenseResponse | null = null) {
  const onSaved = vi.fn();
  const onClose = vi.fn();
  render(
    <ExpenseDialog
      open
      familyId="family-1"
      expense={existing}
      categories={categories}
      members={members}
      currentUserId="user-1"
      onClose={onClose}
      onSaved={onSaved}
    />,
  );
  return { onSaved, onClose };
}

describe('ExpenseDialog', () => {
  it('creates an expense with a comma decimal, today as default date and the current user as payer', async () => {
    vi.mocked(expenseService.create).mockResolvedValue(expense());
    const { onSaved, onClose } = renderDialog();

    await userEvent.type(screen.getByLabelText(en.expenses.dialog.amount), '1500,50');
    await userEvent.selectOptions(screen.getByLabelText(en.expenses.dialog.category), 'cat-fuel');
    await userEvent.click(screen.getByRole('button', { name: en.expenses.dialog.submit }));

    await waitFor(() => expect(onSaved).toHaveBeenCalled());
    expect(expenseService.create).toHaveBeenCalledWith('family-1', {
      amount: 1500.5,
      categoryId: 'cat-fuel',
      date: todayDateOnly(),
      paidByUserId: 'user-1',
      description: null,
    });
    expect(onClose).toHaveBeenCalled();
  });

  it('offers only active categories for a new expense, with translated default names', () => {
    renderDialog();
    const options = Array.from(screen.getByLabelText(en.expenses.dialog.category).querySelectorAll('option')).map((o) => o.textContent);

    expect(options).toEqual([en.expenses.dialog.chooseCategory, en.expenses.defaultCategories.foodGroceries, 'Fuel']);
  });

  it.each([
    ['', en.expenses.validation.amountInvalid],
    ['0', en.expenses.validation.amountInvalid],
    ['12.345', en.expenses.validation.amountInvalid],
    ['10000000.01', en.expenses.validation.amountTooLarge],
  ])('rejects the amount %j without calling the API', async (amount, message) => {
    renderDialog();

    if (amount) await userEvent.type(screen.getByLabelText(en.expenses.dialog.amount), amount);
    await userEvent.click(screen.getByRole('button', { name: en.expenses.dialog.submit }));

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(screen.getByText(en.expenses.validation.required)).toBeInTheDocument(); // no category chosen
    expect(expenseService.create).not.toHaveBeenCalled();
  });

  it('rejects a future date', async () => {
    renderDialog();
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);

    await userEvent.type(screen.getByLabelText(en.expenses.dialog.amount), '100');
    await userEvent.selectOptions(screen.getByLabelText(en.expenses.dialog.category), 'cat-food');
    fireEvent.change(screen.getByLabelText(en.expenses.dialog.date), { target: { value: todayDateOnly(tomorrow) } });
    await userEvent.click(screen.getByRole('button', { name: en.expenses.dialog.submit }));

    expect(await screen.findByText(en.expenses.validation.dateInFuture)).toBeInTheDocument();
    expect(expenseService.create).not.toHaveBeenCalled();
    expect(screen.getByLabelText(en.expenses.dialog.date)).toHaveAttribute('max', todayDateOnly());
  });

  it('edits an expense, keeping its archived category and a payer who has left', async () => {
    vi.mocked(expenseService.update).mockResolvedValue(expense());
    renderDialog(expense());

    expect(screen.getByText(en.expenses.dialog.editTitle)).toBeInTheDocument();
    expect(screen.getByLabelText(en.expenses.dialog.amount)).toHaveValue('1234.50');
    expect(screen.getByLabelText(en.expenses.dialog.category)).toHaveValue('cat-old');
    expect(screen.getByRole('option', { name: en.expenses.filters.archivedOption.replace('{{name}}', 'Old hobby') })).toBeInTheDocument();
    expect(screen.getByLabelText(en.expenses.dialog.paidBy)).toHaveValue('former-user');
    expect(screen.getByRole('option', { name: 'Former Payer' })).toBeInTheDocument();

    await userEvent.clear(screen.getByLabelText(en.expenses.dialog.amount));
    await userEvent.type(screen.getByLabelText(en.expenses.dialog.amount), '99');
    await userEvent.click(screen.getByRole('button', { name: en.expenses.dialog.submit }));

    await waitFor(() =>
      expect(expenseService.update).toHaveBeenCalledWith('family-1', 'exp-1', {
        amount: 99,
        categoryId: 'cat-old',
        date: '2026-10-05',
        paidByUserId: 'former-user',
        description: 'Paint',
      }),
    );
  });

  it('shows Macedonian labels, a comma amount and Macedonian validation messages', async () => {
    await i18n.changeLanguage('mk');
    renderDialog(expense({ amount: 1500.5 }));

    expect(screen.getByText(mk.expenses.dialog.editTitle)).toBeInTheDocument();
    expect(screen.getByLabelText(mk.expenses.dialog.amount)).toHaveValue('1500,50');

    await userEvent.clear(screen.getByLabelText(mk.expenses.dialog.amount));
    await userEvent.click(screen.getByRole('button', { name: mk.expenses.dialog.submit }));

    expect(await screen.findByText(mk.expenses.validation.amountInvalid)).toBeInTheDocument();
  });

  it('shows the server error when saving fails', async () => {
    const { AxiosError, AxiosHeaders } = await import('axios');
    const config = { headers: new AxiosHeaders() } as never;
    vi.mocked(expenseService.create).mockRejectedValue(
      new AxiosError('failed', 'ERR_BAD_REQUEST', config, null, {
        status: 400, data: { message: 'This category is archived and can\'t be used for new expenses.' },
        statusText: '', headers: {}, config,
      }),
    );
    const { onSaved } = renderDialog();

    await userEvent.type(screen.getByLabelText(en.expenses.dialog.amount), '10');
    await userEvent.selectOptions(screen.getByLabelText(en.expenses.dialog.category), 'cat-food');
    await userEvent.click(screen.getByRole('button', { name: en.expenses.dialog.submit }));

    expect(await screen.findByText("This category is archived and can't be used for new expenses.")).toBeInTheDocument();
    expect(onSaved).not.toHaveBeenCalled();
  });
});
