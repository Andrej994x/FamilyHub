import { screen } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { RequireExpensesAccess } from './RequireExpensesAccess';
import { FamilyRole } from '../types';
import { renderWithFamily } from '../test/providers';

function renderAt(role: number) {
  renderWithFamily(
    <Routes>
      <Route path="/dashboard" element={<p>dashboard page</p>} />
      <Route element={<RequireExpensesAccess />}>
        <Route path="/expenses" element={<p>expenses page</p>} />
      </Route>
    </Routes>,
    { role, initialEntries: ['/expenses'] },
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
});
