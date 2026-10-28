import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import Expenses from './Expenses';
import { expenseService } from '../services/expenseService';
import { familyService } from '../services/familyService';
import i18n from '../locales/i18n';
import en from '../locales/en/translation.json';
import mk from '../locales/mk/translation.json';
import { FamilyRole } from '../types';
import type { ExpenseCategoryResponse, ExpenseResponse, ExpenseSummaryResponse, FamilyMemberResponse } from '../types';
import { formatMoney } from '../utils/money';
import { addMonths, currentMonth, formatMonthYear } from '../utils/month';
import { renderWithFamily } from '../test/providers';

vi.mock('../services/expenseService', () => ({
  expenseService: { summary: vi.fn(), list: vi.fn(), categories: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn() },
}));
vi.mock('../services/familyService', () => ({
  familyService: { getMembers: vi.fn() },
}));

const thisMonth = currentMonth();
const day = (d: number) => `${thisMonth.year}-${String(thisMonth.month).padStart(2, '0')}-${String(d).padStart(2, '0')}`;

const categories: ExpenseCategoryResponse[] = [
  { id: 'cat-food', name: 'Food & Groceries', defaultCategory: 0, isArchived: false, sortOrder: 0 },
  { id: 'cat-transport', name: 'Transport', defaultCategory: 2, isArchived: false, sortOrder: 2 },
  { id: 'cat-old', name: 'Old hobby', defaultCategory: null, isArchived: true, sortOrder: 10 },
];

const members: FamilyMemberResponse[] = [
  { id: 'm1', userId: 'owner-1', firstName: 'Ana', lastName: 'Petrova', email: 'a@x', role: FamilyRole.Owner, joinedAt: '' },
  { id: 'm2', userId: 'member-1', firstName: 'Bojan', lastName: 'Petrov', email: 'b@x', role: FamilyRole.Member, joinedAt: '' },
];

function expense(id: string, createdByUserId: string, amount: number, categoryId = 'cat-food'): ExpenseResponse {
  const category = categories.find((c) => c.id === categoryId)!;
  return {
    id, familyId: 'family-1', amount, date: day(3), categoryId, categoryName: category.name,
    categoryDefault: category.defaultCategory, categoryIsArchived: category.isArchived,
    paidByUserId: createdByUserId, paidByName: createdByUserId === 'owner-1' ? 'Ana Petrova' : 'Bojan Petrov',
    description: null, createdByUserId, createdByName: '', createdAt: `${day(3)}T10:00:00Z`, updatedAt: null,
  };
}

const ownersExpense = expense('exp-owner', 'owner-1', 20_000);
const membersExpense = expense('exp-member', 'member-1', 12_000, 'cat-transport');

function summary(overrides: Partial<ExpenseSummaryResponse> = {}): ExpenseSummaryResponse {
  return {
    year: thisMonth.year, month: thisMonth.month, totalSpent: 32_000, expenseCount: 2,
    budget: 50_000, remaining: 18_000, usagePercent: 64,
    categories: [
      { categoryId: 'cat-food', name: 'Food & Groceries', defaultCategory: 0, isArchived: false, spent: 20_000, expenseCount: 1, budget: 15_000, remaining: -5_000, usagePercent: 133 },
      { categoryId: 'cat-transport', name: 'Transport', defaultCategory: 2, isArchived: false, spent: 12_000, expenseCount: 1, budget: null, remaining: null, usagePercent: null },
    ],
    recentExpenses: [ownersExpense, membersExpense],
    ...overrides,
  };
}

function renderPage(role: number, userId: string, entry = '/expenses') {
  renderWithFamily(
    <Routes>
      <Route path="/expenses" element={<Expenses />} />
    </Routes>,
    { role, userId, initialEntries: [entry] },
  );
}

// Intl separates the currency with a no-break space; Testing Library normalizes DOM text to plain spaces.
const plain = (text: string) => text.replace(/\s/g, ' ');
const money = (amount: number) => plain(formatMoney(amount, i18n.language));

beforeEach(() => {
  vi.mocked(expenseService.categories).mockResolvedValue(categories);
  // Like the API, the summary is for the requested month.
  vi.mocked(expenseService.summary).mockImplementation(async (_familyId, year, month) => summary({ year, month }));
  vi.mocked(expenseService.list).mockResolvedValue([ownersExpense, membersExpense]);
  vi.mocked(familyService.getMembers).mockResolvedValue(members);
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('Expenses page', () => {
  it('shows the month total, budget, remaining amount, usage and spending by category in MKD', async () => {
    renderPage(FamilyRole.Owner, 'owner-1');

    const overview = await screen.findByRole('region', { name: en.expenses.summary.title });
    expect(within(overview).getByText(money(32_000))).toBeInTheDocument();
    expect(within(overview).getByText(money(50_000))).toBeInTheDocument();
    expect(within(overview).getByText(money(18_000))).toBeInTheDocument();
    expect(within(overview).getByText(en.expenses.summary.usage.replace('{{percent}}', '64'))).toBeInTheDocument();
    expect(within(overview).getByRole('progressbar')).toHaveAttribute('aria-valuenow', '64');
    expect(expenseService.summary).toHaveBeenCalledWith('family-1', thisMonth.year, thisMonth.month);

    // Translated default category names, the category budget and the over-budget category.
    expect(screen.getAllByText(en.expenses.defaultCategories.foodGroceries).length).toBeGreaterThan(0);
    expect(screen.getByText(en.expenses.byCategory.ofBudget.replace('{{budget}}', money(15_000)))).toBeInTheDocument();
  });

  it('explains when no budget is set and when there are no expenses', async () => {
    vi.mocked(expenseService.summary).mockResolvedValue(
      summary({ totalSpent: 0, expenseCount: 0, budget: null, remaining: null, usagePercent: null, categories: [], recentExpenses: [] }),
    );
    renderPage(FamilyRole.Member, 'member-1');

    expect(await screen.findByText(en.expenses.summary.noBudget)).toBeInTheDocument();
    expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    expect(screen.getByText(en.expenses.byCategory.empty)).toBeInTheDocument();
    expect(screen.getByText(en.expenses.recent.empty)).toBeInTheDocument();
  });

  it('shows "over budget" when spending exceeds the budget', async () => {
    vi.mocked(expenseService.summary).mockResolvedValue(summary({ totalSpent: 60_000, remaining: -10_000, usagePercent: 120 }));
    renderPage(FamilyRole.Owner, 'owner-1');

    const overview = await screen.findByRole('region', { name: en.expenses.summary.title });
    expect(within(overview).getByText(en.expenses.summary.overBudget)).toBeInTheDocument();
    expect(within(overview).getByText(money(10_000))).toBeInTheDocument();
    expect(within(overview).getByRole('progressbar')).toHaveAttribute('aria-valuenow', '100');
  });

  it('lets a member edit and delete only the expenses they added', async () => {
    renderPage(FamilyRole.Member, 'member-1');
    await screen.findByText(en.expenses.recent.title);

    // Each expense renders as a phone card and a desktop row, so its actions appear twice.
    // Accessible names keep Intl's no-break space, so build them from the exact formatted amount.
    const ownLabel = `${en.common.edit}: ${en.expenses.defaultCategories.transport} ${formatMoney(12_000, 'en')}`;
    const othersLabel = `${en.common.edit}: ${en.expenses.defaultCategories.foodGroceries} ${formatMoney(20_000, 'en')}`;
    expect(screen.getAllByRole('button', { name: new RegExp(`^${en.common.edit}:`) })).toHaveLength(2);
    expect(await screen.findAllByRole('button', { name: ownLabel })).toHaveLength(2);
    expect(screen.queryByRole('button', { name: othersLabel })).not.toBeInTheDocument();
  });

  it('lets an owner edit every expense', async () => {
    renderPage(FamilyRole.Owner, 'owner-1');

    expect(await screen.findAllByRole('button', { name: new RegExp(`^${en.common.edit}:`) })).toHaveLength(4);
    expect(screen.getAllByRole('button', { name: new RegExp(`^${en.common.delete}:`) })).toHaveLength(4);
  });

  it('switches to earlier months but not into the future', async () => {
    renderPage(FamilyRole.Owner, 'owner-1');
    await screen.findByText(formatMonthYear(thisMonth, 'en'));

    expect(screen.getByRole('button', { name: en.expenses.month.next })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.previous }));

    const previous = addMonths(thisMonth, -1);
    expect(await screen.findByText(formatMonthYear(previous, 'en'))).toBeInTheDocument();
    await waitFor(() => expect(expenseService.summary).toHaveBeenLastCalledWith('family-1', previous.year, previous.month));
    expect(screen.getByRole('button', { name: en.expenses.month.next })).toBeEnabled();
  });

  it('filters the history by category and payer for the selected month', async () => {
    renderPage(FamilyRole.Owner, 'owner-1');
    await userEvent.click(await screen.findByRole('tab', { name: en.expenses.tabs.history }));

    await waitFor(() =>
      expect(expenseService.list).toHaveBeenLastCalledWith('family-1', {
        year: thisMonth.year, month: thisMonth.month, categoryId: undefined, paidByUserId: undefined,
      }),
    );
    expect(await screen.findByText(en.expenses.history.total_other.replace('{{count}}', '2').replace('{{amount}}', money(32_000)))).toBeInTheDocument();

    // Archived categories stay filterable for old expenses.
    expect(screen.getByRole('option', { name: en.expenses.filters.archivedOption.replace('{{name}}', 'Old hobby') })).toBeInTheDocument();
    await userEvent.selectOptions(screen.getByLabelText(en.expenses.filters.category), 'cat-transport');
    await userEvent.selectOptions(screen.getByLabelText(en.expenses.filters.payer), 'member-1');

    await waitFor(() =>
      expect(expenseService.list).toHaveBeenLastCalledWith('family-1', {
        year: thisMonth.year, month: thisMonth.month, categoryId: 'cat-transport', paidByUserId: 'member-1',
      }),
    );
    expect(screen.getByRole('button', { name: en.expenses.filters.clear })).toBeInTheDocument();
  });

  it('says when no expenses match the filters', async () => {
    vi.mocked(expenseService.list).mockResolvedValue([]);
    renderPage(FamilyRole.Owner, 'owner-1');
    await userEvent.click(await screen.findByRole('tab', { name: en.expenses.tabs.history }));
    expect(await screen.findByText(en.expenses.empty)).toBeInTheDocument();

    await userEvent.selectOptions(screen.getByLabelText(en.expenses.filters.category), 'cat-food');

    expect(await screen.findByText(en.expenses.emptyFiltered)).toBeInTheDocument();
  });

  it('deletes an expense after confirmation and reloads the summary', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    vi.mocked(expenseService.remove).mockResolvedValue();
    renderPage(FamilyRole.Owner, 'owner-1');
    const [deleteButton] = await screen.findAllByRole('button', { name: new RegExp(`^${en.common.delete}:`) });
    const callsBefore = vi.mocked(expenseService.summary).mock.calls.length;

    await userEvent.click(deleteButton);

    await waitFor(() => expect(expenseService.remove).toHaveBeenCalledWith('family-1', 'exp-owner'));
    await waitFor(() => expect(vi.mocked(expenseService.summary).mock.calls.length).toBeGreaterThan(callsBefore));
  });

  it('shows a load error and clears it once the next month loads', async () => {
    vi.mocked(expenseService.summary).mockRejectedValueOnce(new Error('offline'));
    renderPage(FamilyRole.Owner, 'owner-1');

    expect(await screen.findByText(en.expenses.errors.loadFailed)).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.previous }));

    await waitFor(() => expect(screen.queryByText(en.expenses.errors.loadFailed)).not.toBeInTheDocument());
    expect(await screen.findByRole('region', { name: en.expenses.summary.title })).toBeInTheDocument();
  });

  it("never shows the previous month's numbers when the new month fails to load", async () => {
    renderPage(FamilyRole.Owner, 'owner-1');
    const overview = await screen.findByRole('region', { name: en.expenses.summary.title });
    expect(within(overview).getByText(money(32_000))).toBeInTheDocument();

    vi.mocked(expenseService.summary).mockRejectedValueOnce(new Error('offline'));
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.previous }));

    expect(await screen.findByText(en.expenses.errors.loadFailed)).toBeInTheDocument();
    expect(screen.getByText(formatMonthYear(addMonths(thisMonth, -1), 'en'))).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: en.expenses.summary.title })).not.toBeInTheDocument();
    expect(screen.queryByText(money(32_000))).not.toBeInTheDocument();
    expect(screen.queryByText(en.expenses.recent.title)).not.toBeInTheDocument();
  });

  it("doesn't show the previous month's numbers while the new month is loading", async () => {
    renderPage(FamilyRole.Owner, 'owner-1');
    await screen.findByRole('region', { name: en.expenses.summary.title });

    vi.mocked(expenseService.summary).mockReturnValueOnce(new Promise(() => {}));
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.previous }));

    expect(await screen.findByText(en.common.loading)).toBeInTheDocument();
    expect(screen.queryByText(money(32_000))).not.toBeInTheDocument();
  });

  it("never shows the previous month's history, or an empty state, when the new month fails to load", async () => {
    renderPage(FamilyRole.Owner, 'owner-1');
    await userEvent.click(await screen.findByRole('tab', { name: en.expenses.tabs.history }));
    expect(await screen.findAllByRole('button', { name: new RegExp(`^${en.common.edit}:`) })).toHaveLength(4);

    vi.mocked(expenseService.list).mockRejectedValueOnce(new Error('offline'));
    await userEvent.click(screen.getByRole('button', { name: en.expenses.month.previous }));

    expect(await screen.findByText(en.expenses.errors.loadFailed)).toBeInTheDocument();
    expect(screen.queryAllByRole('button', { name: new RegExp(`^${en.common.edit}:`) })).toHaveLength(0);
    expect(screen.queryByText(en.expenses.empty)).not.toBeInTheDocument();
  });

  it('opens the add dialog from the dashboard quick action link', async () => {
    renderPage(FamilyRole.Member, 'member-1', '/expenses?add=1');

    expect(await screen.findByRole('heading', { name: en.expenses.dialog.addTitle })).toBeInTheDocument();
  });

  it('renders in Macedonian with MKD amounts in the Macedonian format', async () => {
    await i18n.changeLanguage('mk');
    renderPage(FamilyRole.Owner, 'owner-1');

    expect(await screen.findByRole('heading', { name: mk.nav.expenses })).toBeInTheDocument();
    const overview = await screen.findByRole('region', { name: mk.expenses.summary.title });
    expect(within(overview).getByText(plain(formatMoney(32_000, 'mk')))).toBeInTheDocument();
    expect(within(overview).getByText(mk.expenses.summary.remaining)).toBeInTheDocument();
    expect(screen.getAllByText(mk.expenses.defaultCategories.foodGroceries).length).toBeGreaterThan(0);
    expect(screen.getByText(formatMonthYear(thisMonth, 'mk'))).toBeInTheDocument();
  });
});
