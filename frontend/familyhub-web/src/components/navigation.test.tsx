import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { BottomNav } from './BottomNav';
import { Sidebar } from './Sidebar';
import { QuickActions } from './dashboard/QuickActions';
import { FamilyRole } from '../types';
import en from '../locales/en/translation.json';
import { renderWithFamily } from '../test/providers';

describe('Family Expenses navigation', () => {
  it.each([FamilyRole.Owner, FamilyRole.Parent, FamilyRole.Member])('shows Expenses in the sidebar for role %i', (role) => {
    renderWithFamily(<Sidebar />, { role });

    expect(screen.getByRole('link', { name: en.nav.expenses })).toHaveAttribute('href', '/expenses');
  });

  it('hides Expenses from children in the sidebar but keeps the other items', () => {
    renderWithFamily(<Sidebar />, { role: FamilyRole.Child });

    expect(screen.queryByRole('link', { name: en.nav.expenses })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: en.nav.tasks })).toBeInTheDocument();
  });

  it('puts Expenses under More on mobile, not in the bottom bar', async () => {
    renderWithFamily(<BottomNav />, { role: FamilyRole.Member });
    const bar = screen.getByRole('navigation');

    expect(within(bar).queryByRole('link', { name: en.nav.expenses })).not.toBeInTheDocument();
    await userEvent.click(within(bar).getByRole('button', { name: en.nav.more }));

    expect(screen.getByRole('link', { name: en.nav.expenses })).toHaveAttribute('href', '/expenses');
  });

  it('hides Expenses from children under More', async () => {
    renderWithFamily(<BottomNav />, { role: FamilyRole.Child });

    await userEvent.click(screen.getByRole('button', { name: en.nav.more }));

    expect(screen.queryByRole('link', { name: en.nav.expenses })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: en.nav.vault })).toBeInTheDocument();
  });

  it('offers the Add expense quick action to adults only', () => {
    const { unmount } = renderWithFamily(<QuickActions />, { role: FamilyRole.Member });
    expect(screen.getByRole('button', { name: en.dashboard.quick.addExpense })).toBeInTheDocument();
    unmount();

    renderWithFamily(<QuickActions />, { role: FamilyRole.Child });
    expect(screen.queryByRole('button', { name: en.dashboard.quick.addExpense })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: en.dashboard.quick.addTask })).toBeInTheDocument();
  });
});
