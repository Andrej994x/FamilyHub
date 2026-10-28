import { screen } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { RequireExpensesAccess } from './RequireExpensesAccess';
import { FamilyRole } from '../types';
import { renderWithFamily } from '../test/providers';

function renderAt(role: number, path = '/expenses') {
  renderWithFamily(
    <Routes>
      <Route path="/dashboard" element={<p>dashboard page</p>} />
      <Route element={<RequireExpensesAccess />}>
        <Route path="/expenses" element={<p>expenses page</p>} />
        <Route path="/expenses/settings" element={<p>settings page</p>} />
      </Route>
    </Routes>,
    { role, initialEntries: [path] },
  );
}

describe('RequireExpensesAccess', () => {
  it.each([FamilyRole.Owner, FamilyRole.Parent, FamilyRole.Member])('lets role %i open Expenses', (role) => {
    renderAt(role);

    expect(screen.getByText('expenses page')).toBeInTheDocument();
  });

  it('sends children to the dashboard', () => {
    renderAt(FamilyRole.Child);

    expect(screen.getByText('dashboard page')).toBeInTheDocument();
    expect(screen.queryByText('expenses page')).not.toBeInTheDocument();
  });

  it('also keeps children out of budgets & categories', () => {
    renderAt(FamilyRole.Child, '/expenses/settings');

    expect(screen.getByText('dashboard page')).toBeInTheDocument();
    expect(screen.queryByText('settings page')).not.toBeInTheDocument();
  });
});
