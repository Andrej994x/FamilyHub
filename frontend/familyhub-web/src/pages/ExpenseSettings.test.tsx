import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios';
import { Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import ExpenseSettings from './ExpenseSettings';
import { expenseService } from '../services/expenseService';
import i18n from '../locales/i18n';
import en from '../locales/en/translation.json';
import mk from '../locales/mk/translation.json';
import { FamilyRole } from '../types';
import type { ExpenseBudgetResponse, ExpenseCategoryResponse } from '../types';
import { formatMoney } from '../utils/money';
import { addMonths, formatMonthYear } from '../utils/month';
import { renderWithFamily } from '../test/providers';

vi.mock('../services/expenseService', () => ({
  expenseService: {
    categories: vi.fn(),
    budgets: vi.fn(),
    setBudgets: vi.fn(),
    createCategory: vi.fn(),
    updateCategory: vi.fn(),
    deleteCategory: vi.fn(),
  },
}));

const s = en.expenses.settings;
const october = { year: 2026, month: 10 };

const categories: ExpenseCategoryResponse[] = [
  { id: 'cat-food', name: 'Food & Groceries', defaultCategory: 0, isArchived: false, sortOrder: 0 },
  { id: 'cat-transport', name: 'Transport', defaultCategory: 2, isArchived: false, sortOrder: 2 },
  { id: 'cat-fuel', name: 'Fuel', defaultCategory: null, isArchived: false, sortOrder: 9 },
  { id: 'cat-old', name: 'Old hobby', defaultCategory: null, isArchived: true, sortOrder: 10 },
];

function budgets(year: number, month: number, overrides: Partial<ExpenseBudgetResponse> = {}): ExpenseBudgetResponse {
  return { year, month, overallAmount: 50_000, categories: [{ categoryId: 'cat-food', amount: 15_000 }], ...overrides };
}

function apiError(status: number, message: string): AxiosError {
  const config = { headers: new AxiosHeaders() } as InternalAxiosRequestConfig;
  return new AxiosError('failed', 'ERR_BAD_REQUEST', config, null, { status, data: { message }, statusText: '', headers: {}, config });
}

function renderPage(role: number, entry = '/expenses/settings?year=2026&month=10') {
  renderWithFamily(
    <Routes>
      <Route path="/expenses/settings" element={<ExpenseSettings />} />
      <Route path="/expenses" element={<p>expenses page</p>} />
    </Routes>,
    { role, userId: 'user-1', initialEntries: [entry] },
  );
}

// Intl separates the currency with a no-break space; Testing Library normalizes DOM text to plain spaces.
const money = (amount: number) => formatMoney(amount, i18n.language).replace(/\s/g, ' ');
const field = (name: string) => screen.getByLabelText(name) as HTMLInputElement;

beforeEach(() => {
  vi.mocked(expenseService.categories).mockResolvedValue(categories);
  // Like the API, the budgets are for the requested month.
  vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) => budgets(year, month));
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('Expense settings: budgets', () => {
  it('opens on the linked month with the budgets in effect filled in', async () => {
    renderPage(FamilyRole.Owner);

    expect(await screen.findByText(formatMonthYear(october, 'en'))).toBeInTheDocument();
    expect(expenseService.budgets).toHaveBeenCalledWith('family-1', 2026, 10);
    expect(await screen.findByLabelText(s.budgets.overall)).toHaveValue('50000');
    expect(field(en.expenses.defaultCategories.foodGroceries)).toHaveValue('15000');
    expect(field(en.expenses.defaultCategories.transport)).toHaveValue('');
    // Only active categories get a budget field.
    expect(screen.queryByLabelText('Old hobby')).not.toBeInTheDocument();
    expect(screen.getByText(s.budgets.appliesFrom.replace('{{month}}', formatMonthYear(october, 'en')))).toBeInTheDocument();
  });

  it('saves the complete set from the selected month: comma decimals, empty fields omitted', async () => {
    vi.mocked(expenseService.setBudgets).mockImplementation(async (_familyId, payload) => ({
      year: payload.year, month: payload.month, overallAmount: payload.overallAmount,
      categories: payload.categories.map((c) => ({ categoryId: c.categoryId, amount: c.amount! })),
    }));
    renderPage(FamilyRole.Parent);

    const overall = await screen.findByLabelText(s.budgets.overall);
    await userEvent.clear(overall);
    await userEvent.type(overall, '45000,50');
    await userEvent.clear(field(en.expenses.defaultCategories.foodGroceries));
    await userEvent.type(field(en.expenses.defaultCategories.transport), '6000');
    await userEvent.type(field('Fuel'), '2500.25');
    expect(screen.getByText(s.budgets.categoryTotal.replace('{{amount}}', money(8_500.25)))).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: s.budgets.save }));

    expect(await screen.findByRole('status')).toHaveTextContent(s.budgets.saved);
    expect(expenseService.setBudgets).toHaveBeenCalledWith('family-1', {
      year: 2026,
      month: 10,
      overallAmount: 45_000.5,
      categories: [
        { categoryId: 'cat-transport', amount: 6_000 },
        { categoryId: 'cat-fuel', amount: 2_500.25 },
      ],
    });
  });

  it('removes the overall budget when its field is cleared', async () => {
    vi.mocked(expenseService.setBudgets).mockResolvedValue(budgets(2026, 10, { overallAmount: null }));
    renderPage(FamilyRole.Owner);

    await userEvent.clear(await screen.findByLabelText(s.budgets.overall));
    await userEvent.click(screen.getByRole('button', { name: s.budgets.save }));

    await waitFor(() =>
      expect(expenseService.setBudgets).toHaveBeenCalledWith('family-1', expect.objectContaining({ overallAmount: null })),
    );
  });

  it.each([
    ['12.345', en.expenses.validation.amountInvalid],
    ['0', en.expenses.validation.amountInvalid],
    ['10000001', en.expenses.validation.amountTooLarge],
  ])('rejects the budget %j without saving', async (value, message) => {
    renderPage(FamilyRole.Owner);

    const overall = await screen.findByLabelText(s.budgets.overall);
    await userEvent.clear(overall);
    await userEvent.type(overall, value);
    await userEvent.click(screen.getByRole('button', { name: s.budgets.save }));

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(overall).toHaveAttribute('aria-invalid', 'true');
    expect(expenseService.setBudgets).not.toHaveBeenCalled();
  });

  it('shows the server error when saving fails', async () => {
    vi.mocked(expenseService.setBudgets).mockRejectedValue(
      apiError(409, 'The budgets were changed at the same time by someone else. Reload and try again.'),
    );
    renderPage(FamilyRole.Owner);

    await userEvent.click(await screen.findByRole('button', { name: s.budgets.save }));

    expect(await screen.findByText('The budgets were changed at the same time by someone else. Reload and try again.')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('can move to future months to plan budgets ahead', async () => {
    renderPage(FamilyRole.Owner);
    await screen.findByLabelText(s.budgets.overall);

    const next = addMonths(october, 1);
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.next }));
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.next }));

    await waitFor(() => expect(expenseService.budgets).toHaveBeenLastCalledWith('family-1', 2026, 12));
    expect(screen.getByText(formatMonthYear(addMonths(next, 1), 'en'))).toBeInTheDocument();
  });

  it("never shows another month's budgets when a month fails to load", async () => {
    renderPage(FamilyRole.Owner);
    await screen.findByLabelText(s.budgets.overall);

    vi.mocked(expenseService.budgets).mockRejectedValueOnce(new Error('offline'));
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.previous }));

    expect(await screen.findByText(s.errors.loadFailed)).toBeInTheDocument();
    expect(screen.queryByLabelText(s.budgets.overall)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: s.budgets.save })).not.toBeInTheDocument();
  });

  it("doesn't offer the budget form when categories fail to load, so a save can't remove category budgets", async () => {
    vi.mocked(expenseService.categories).mockRejectedValueOnce(new Error('offline'));
    renderPage(FamilyRole.Owner);

    expect(await screen.findByText(s.errors.loadFailed)).toBeInTheDocument();
    // The budget section isn't rendered at all (its title would appear at once, before its data).
    expect(screen.queryByText(s.budgets.title)).not.toBeInTheDocument();
    expect(screen.queryByText(s.categories.title)).not.toBeInTheDocument();
    expect(expenseService.budgets).not.toHaveBeenCalled();
    expect(expenseService.setBudgets).not.toHaveBeenCalled();

    // Retrying loads the categories and then the form.
    await userEvent.click(screen.getByRole('button', { name: en.common.retry }));
    expect(await screen.findByLabelText(s.budgets.overall)).toHaveValue('50000');
    expect(screen.queryByText(s.errors.loadFailed)).not.toBeInTheDocument();
  });

  it('keeps budgets of categories this page does not know about when saving', async () => {
    // E.g. a category another manager added (with a budget) after this page loaded.
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      budgets(year, month, { categories: [{ categoryId: 'cat-food', amount: 15_000 }, { categoryId: 'cat-new', amount: 7_000 }] }),
    );
    vi.mocked(expenseService.setBudgets).mockImplementation(async (_familyId, payload) => ({
      year: payload.year, month: payload.month, overallAmount: payload.overallAmount,
      categories: payload.categories.map((c) => ({ categoryId: c.categoryId, amount: c.amount! })),
    }));
    renderPage(FamilyRole.Owner);

    await userEvent.click(await screen.findByRole('button', { name: s.budgets.save }));

    await waitFor(() =>
      expect(expenseService.setBudgets).toHaveBeenCalledWith('family-1', {
        year: 2026,
        month: 10,
        overallAmount: 50_000,
        categories: [
          { categoryId: 'cat-food', amount: 15_000 },
          { categoryId: 'cat-new', amount: 7_000 },
        ],
      }),
    );
  });

  it("ignores a save that finishes after switching months, so it can't overwrite the new month", async () => {
    let finishSave: (value: ExpenseBudgetResponse) => void = () => {};
    vi.mocked(expenseService.setBudgets).mockReturnValueOnce(new Promise((resolve) => (finishSave = resolve)));
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      month === 11 ? budgets(year, month, { overallAmount: 60_000, categories: [] }) : budgets(year, month),
    );
    renderPage(FamilyRole.Owner);

    const overall = await screen.findByLabelText(s.budgets.overall);
    await userEvent.clear(overall);
    await userEvent.type(overall, '1');
    await userEvent.click(screen.getByRole('button', { name: s.budgets.save }));
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.next }));
    await waitFor(() => expect(screen.getByLabelText(s.budgets.overall)).toHaveValue('60000'));

    // October's save completes only now.
    finishSave(budgets(2026, 10, { overallAmount: 1, categories: [] }));

    await waitFor(() => expect(screen.getByRole('button', { name: s.budgets.save })).toBeEnabled());
    expect(screen.getByText(formatMonthYear({ year: 2026, month: 11 }, 'en'))).toBeInTheDocument();
    expect(screen.getByLabelText(s.budgets.overall)).toHaveValue('60000');
    expect(screen.getByLabelText(en.expenses.defaultCategories.foodGroceries)).toHaveValue('');
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it("reloads budgets after a category is deleted, so a later save doesn't send its removed budget", async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    // The server removes a deleted category's budget rows; the mocks follow its state.
    let oldHobbyExists = true;
    vi.mocked(expenseService.categories).mockImplementation(async () =>
      categories.filter((c) => c.id !== 'cat-old' || oldHobbyExists),
    );
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      budgets(year, month, {
        categories: [{ categoryId: 'cat-food', amount: 15_000 }, ...(oldHobbyExists ? [{ categoryId: 'cat-old', amount: 3_000 }] : [])],
      }),
    );
    vi.mocked(expenseService.deleteCategory).mockImplementation(async () => {
      oldHobbyExists = false;
    });
    vi.mocked(expenseService.setBudgets).mockImplementation(async (_familyId, payload) => ({
      year: payload.year, month: payload.month, overallAmount: payload.overallAmount,
      categories: payload.categories.map((c) => ({ categoryId: c.categoryId, amount: c.amount! })),
    }));
    renderPage(FamilyRole.Owner);

    const archived = await screen.findByRole('list', { name: s.categories.archivedTitle.replace('{{count}}', '1') });
    await userEvent.click(within(archived).getByRole('button', { name: `${en.common.delete}: Old hobby` }));
    await waitFor(() => expect(screen.queryByText(s.budgets.archivedRemoved)).not.toBeInTheDocument());
    await userEvent.click(screen.getByRole('button', { name: s.budgets.save }));

    await waitFor(() =>
      expect(expenseService.setBudgets).toHaveBeenCalledWith('family-1', {
        year: 2026,
        month: 10,
        overallAmount: 50_000,
        categories: [{ categoryId: 'cat-food', amount: 15_000 }],
      }),
    );
  });

  it('reloads a later month after an earlier month’s save completes, since budgets carry forward', async () => {
    // October's save carries into November, which has no budget change of its own.
    let serverOverall = 50_000;
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      budgets(year, month, { overallAmount: serverOverall, categories: [] }),
    );
    let finishSave: () => void = () => {};
    vi.mocked(expenseService.setBudgets).mockImplementationOnce(
      (_familyId, payload) =>
        new Promise((resolve) => {
          finishSave = () => {
            serverOverall = payload.overallAmount!;
            resolve(budgets(payload.year, payload.month, { overallAmount: payload.overallAmount, categories: [] }));
          };
        }),
    );
    renderPage(FamilyRole.Owner);

    const overall = await screen.findByLabelText(s.budgets.overall);
    await userEvent.clear(overall);
    await userEvent.type(overall, '70000');
    await userEvent.click(screen.getByRole('button', { name: s.budgets.save }));
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.next }));
    // November loaded before October's save finished, so it still shows the old carried-forward value.
    await waitFor(() => expect(screen.getByLabelText(s.budgets.overall)).toHaveValue('50000'));

    finishSave();

    await waitFor(() => expect(screen.getByLabelText(s.budgets.overall)).toHaveValue('70000'));
    expect(screen.getByText(formatMonthYear({ year: 2026, month: 11 }, 'en'))).toBeInTheDocument();
    expect(expenseService.budgets).toHaveBeenLastCalledWith('family-1', 2026, 11);
  });

  it('keeps unsaved budget edits when categories reload after adding a category', async () => {
    const pets: ExpenseCategoryResponse = { id: 'cat-pets', name: 'Pets', defaultCategory: null, isArchived: false, sortOrder: 11 };
    let petsExists = false;
    vi.mocked(expenseService.categories).mockImplementation(async () => (petsExists ? [...categories, pets] : categories));
    vi.mocked(expenseService.createCategory).mockImplementation(async () => {
      petsExists = true;
      return pets;
    });
    // After the reload the server reports a changed Food budget; untouched fields pick it up.
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      budgets(year, month, { categories: [{ categoryId: 'cat-food', amount: petsExists ? 16_000 : 15_000 }] }),
    );
    renderPage(FamilyRole.Owner);

    const overall = await screen.findByLabelText(s.budgets.overall);
    await userEvent.clear(overall);
    await userEvent.type(overall, '45000');
    await userEvent.type(field(en.expenses.defaultCategories.transport), '6000');
    await userEvent.click(screen.getByRole('button', { name: s.categories.add }));
    await userEvent.type(screen.getByLabelText(s.categories.name), 'Pets');
    await userEvent.click(screen.getByRole('button', { name: s.categories.submit }));

    expect(await screen.findByLabelText('Pets')).toHaveValue('');
    await waitFor(() => expect(field(en.expenses.defaultCategories.foodGroceries)).toHaveValue('16000'));
    expect(field(s.budgets.overall)).toHaveValue('45000');
    expect(field(en.expenses.defaultCategories.transport)).toHaveValue('6000');
  });

  it('locks the budget fields while a save is pending, so newer edits cannot be overwritten', async () => {
    let finishSave: (value: ExpenseBudgetResponse) => void = () => {};
    vi.mocked(expenseService.setBudgets).mockReturnValueOnce(new Promise((resolve) => (finishSave = resolve)));
    renderPage(FamilyRole.Owner);

    const overall = await screen.findByLabelText(s.budgets.overall);
    await userEvent.clear(overall);
    await userEvent.type(overall, '45000');
    await userEvent.click(screen.getByRole('button', { name: s.budgets.save }));

    expect(overall).toBeDisabled();
    expect(field(en.expenses.defaultCategories.foodGroceries)).toBeDisabled();
    await userEvent.type(field(en.expenses.defaultCategories.transport), '999');
    expect(field(en.expenses.defaultCategories.transport)).toHaveValue('');

    finishSave(budgets(2026, 10, { overallAmount: 45_000 }));

    await waitFor(() => expect(overall).toBeEnabled());
    expect(overall).toHaveValue('45000');
    expect(await screen.findByRole('status')).toHaveTextContent(s.budgets.saved);
  });

  it('warns that budgets of archived categories are removed on save', async () => {
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      budgets(year, month, { categories: [{ categoryId: 'cat-old', amount: 3_000 }] }),
    );
    renderPage(FamilyRole.Owner);

    expect(await screen.findByText(s.budgets.archivedRemoved)).toBeInTheDocument();
  });
});

describe('Expense settings: members', () => {
  it('shows budgets and categories read-only to members', async () => {
    renderPage(FamilyRole.Member);

    expect(await screen.findByText(s.readOnly)).toBeInTheDocument();
    expect(await screen.findByText(money(50_000))).toBeInTheDocument();
    expect(screen.getByText(money(15_000))).toBeInTheDocument();
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: s.budgets.save })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: s.categories.add })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: new RegExp(`^${s.categories.archive}:`) })).not.toBeInTheDocument();
    const list = screen.getByRole('list', { name: s.categories.title });
    expect(within(list).getByText(en.expenses.defaultCategories.transport)).toBeInTheDocument();
  });

  it('tells members when no budget is set', async () => {
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      budgets(year, month, { overallAmount: null, categories: [] }),
    );
    renderPage(FamilyRole.Member);

    expect(await screen.findByText(s.budgets.noneSet)).toBeInTheDocument();
  });
});

describe('Expense settings: categories', () => {
  it('adds a category and reloads the list', async () => {
    vi.mocked(expenseService.createCategory).mockResolvedValue({ id: 'cat-new', name: 'Pets', defaultCategory: null, isArchived: false, sortOrder: 11 });
    renderPage(FamilyRole.Owner);

    await userEvent.click(await screen.findByRole('button', { name: s.categories.add }));
    await userEvent.type(screen.getByLabelText(s.categories.name), '  Pets  ');
    await userEvent.click(screen.getByRole('button', { name: s.categories.submit }));

    await waitFor(() => expect(expenseService.createCategory).toHaveBeenCalledWith('family-1', { name: 'Pets' }));
    await waitFor(() => expect(expenseService.categories).toHaveBeenCalledTimes(2));
    expect(screen.queryByRole('heading', { name: s.categories.addTitle })).not.toBeInTheDocument();
  });

  it('rejects an empty or too long name and shows a duplicate-name error from the server', async () => {
    vi.mocked(expenseService.createCategory).mockRejectedValue(apiError(409, 'A category with this name already exists (it may be archived).'));
    renderPage(FamilyRole.Owner);

    await userEvent.click(await screen.findByRole('button', { name: s.categories.add }));
    await userEvent.click(screen.getByRole('button', { name: s.categories.submit }));
    expect(await screen.findByText(s.categories.nameInvalid)).toBeInTheDocument();
    await userEvent.type(screen.getByLabelText(s.categories.name), 'a'.repeat(51));
    await userEvent.click(screen.getByRole('button', { name: s.categories.submit }));
    expect(screen.getByText(s.categories.nameInvalid)).toBeInTheDocument();
    expect(expenseService.createCategory).not.toHaveBeenCalled();

    await userEvent.clear(screen.getByLabelText(s.categories.name));
    await userEvent.type(screen.getByLabelText(s.categories.name), 'Transport');
    await userEvent.click(screen.getByRole('button', { name: s.categories.submit }));
    expect(await screen.findByText('A category with this name already exists (it may be archived).')).toBeInTheDocument();
  });

  it('renames a category, but saves nothing when a built-in name is left unchanged', async () => {
    vi.mocked(expenseService.updateCategory).mockResolvedValue(categories[1]);
    renderPage(FamilyRole.Owner);
    const renameTransport = `${s.categories.rename}: ${en.expenses.defaultCategories.transport}`;

    await userEvent.click(await screen.findByRole('button', { name: renameTransport }));
    expect(screen.getByLabelText(s.categories.name)).toHaveValue(en.expenses.defaultCategories.transport);
    expect(screen.getByText(s.categories.builtInHint)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: s.categories.submit }));
    expect(expenseService.updateCategory).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: renameTransport }));
    await userEvent.clear(screen.getByLabelText(s.categories.name));
    await userEvent.type(screen.getByLabelText(s.categories.name), 'Car & fuel');
    await userEvent.click(screen.getByRole('button', { name: s.categories.submit }));

    await waitFor(() =>
      expect(expenseService.updateCategory).toHaveBeenCalledWith('family-1', 'cat-transport', { name: 'Car & fuel', isArchived: false }),
    );
  });

  it('archives with the stored name so a built-in category stays translated', async () => {
    // In Macedonian the label differs from the stored (English) name, so sending the label would rename it.
    await i18n.changeLanguage('mk');
    vi.mocked(expenseService.updateCategory).mockResolvedValue({ ...categories[0], isArchived: true });
    renderPage(FamilyRole.Owner);

    await userEvent.click(
      await screen.findByRole('button', {
        name: `${mk.expenses.settings.categories.archive}: ${mk.expenses.defaultCategories.foodGroceries}`,
      }),
    );

    await waitFor(() =>
      expect(expenseService.updateCategory).toHaveBeenCalledWith('family-1', 'cat-food', { name: 'Food & Groceries', isArchived: true }),
    );
    await waitFor(() => expect(expenseService.categories).toHaveBeenCalledTimes(2));
  });

  it('shows why the last active category cannot be archived', async () => {
    vi.mocked(expenseService.updateCategory).mockRejectedValue(apiError(400, 'The family needs at least one active category.'));
    renderPage(FamilyRole.Owner);

    await userEvent.click(await screen.findByRole('button', { name: `${s.categories.archive}: Fuel` }));

    expect(await screen.findByText('The family needs at least one active category.')).toBeInTheDocument();
  });

  it('restores an archived category and deletes one after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    vi.mocked(expenseService.updateCategory).mockResolvedValue({ ...categories[3], isArchived: false });
    vi.mocked(expenseService.deleteCategory).mockRejectedValueOnce(apiError(409, 'This category is used by expenses. Archive it instead.'));
    renderPage(FamilyRole.Owner);

    const archived = await screen.findByRole('list', { name: s.categories.archivedTitle.replace('{{count}}', '1') });
    await userEvent.click(within(archived).getByRole('button', { name: `${s.categories.restore}: Old hobby` }));
    await waitFor(() =>
      expect(expenseService.updateCategory).toHaveBeenCalledWith('family-1', 'cat-old', { name: 'Old hobby', isArchived: false }),
    );

    await userEvent.click(within(archived).getByRole('button', { name: `${en.common.delete}: Old hobby` }));
    await waitFor(() => expect(expenseService.deleteCategory).toHaveBeenCalledWith('family-1', 'cat-old'));
    expect(await screen.findByText('This category is used by expenses. Archive it instead.')).toBeInTheDocument();
  });
});

describe('Expense settings: language', () => {
  it('renders in Macedonian with comma decimals and translated categories', async () => {
    await i18n.changeLanguage('mk');
    vi.mocked(expenseService.budgets).mockImplementation(async (_familyId, year, month) =>
      budgets(year, month, { overallAmount: 45_000.5 }),
    );
    renderPage(FamilyRole.Owner);

    expect(await screen.findByRole('heading', { name: mk.expenses.settings.title })).toBeInTheDocument();
    expect(await screen.findByLabelText(mk.expenses.settings.budgets.overall)).toHaveValue('45000,50');
    expect(screen.getByLabelText(mk.expenses.defaultCategories.foodGroceries)).toHaveValue('15000');
    expect(screen.getByRole('button', { name: mk.expenses.settings.budgets.save })).toBeInTheDocument();
  });

  it('links back to the expenses page', async () => {
    renderPage(FamilyRole.Member);

    await userEvent.click(await screen.findByRole('link', { name: s.back }));

    expect(screen.getByText('expenses page')).toBeInTheDocument();
  });
});
