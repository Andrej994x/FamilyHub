import apiClient from '../api/client';
import type {
  ExpenseBudgetResponse,
  ExpenseCategoryRequest,
  ExpenseCategoryResponse,
  ExpenseFilter,
  ExpenseRequest,
  ExpenseResponse,
  ExpenseSummaryResponse,
  SetExpenseBudgetsRequest,
  UpdateExpenseCategoryRequest,
} from '../types';

export const expenseService = {
  async list(familyId: string, filter: ExpenseFilter): Promise<ExpenseResponse[]> {
    const { data } = await apiClient.get<ExpenseResponse[]>(`/families/${familyId}/expenses`, {
      params: filter,
    });
    return data;
  },

  async summary(familyId: string, year: number, month: number): Promise<ExpenseSummaryResponse> {
    const { data } = await apiClient.get<ExpenseSummaryResponse>(
      `/families/${familyId}/expenses/summary`,
      { params: { year, month } },
    );
    return data;
  },

  async create(familyId: string, payload: ExpenseRequest): Promise<ExpenseResponse> {
    const { data } = await apiClient.post<ExpenseResponse>(`/families/${familyId}/expenses`, payload);
    return data;
  },

  async update(familyId: string, expenseId: string, payload: ExpenseRequest): Promise<ExpenseResponse> {
    const { data } = await apiClient.put<ExpenseResponse>(
      `/families/${familyId}/expenses/${expenseId}`,
      payload,
    );
    return data;
  },

  async remove(familyId: string, expenseId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/expenses/${expenseId}`);
  },

  /** The family's categories (the API creates the defaults on first use). */
  async categories(familyId: string, includeArchived = false): Promise<ExpenseCategoryResponse[]> {
    const { data } = await apiClient.get<ExpenseCategoryResponse[]>(
      `/families/${familyId}/expense-categories`,
      { params: { includeArchived } },
    );
    return data;
  },

  async createCategory(familyId: string, payload: ExpenseCategoryRequest): Promise<ExpenseCategoryResponse> {
    const { data } = await apiClient.post<ExpenseCategoryResponse>(
      `/families/${familyId}/expense-categories`,
      payload,
    );
    return data;
  },

  async updateCategory(
    familyId: string,
    categoryId: string,
    payload: UpdateExpenseCategoryRequest,
  ): Promise<ExpenseCategoryResponse> {
    const { data } = await apiClient.put<ExpenseCategoryResponse>(
      `/families/${familyId}/expense-categories/${categoryId}`,
      payload,
    );
    return data;
  },

  /** Only an unused category can be deleted; a used one must be archived (the API returns 409). */
  async deleteCategory(familyId: string, categoryId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/expense-categories/${categoryId}`);
  },

  /** The budgets in effect for a month (carried forward from the latest change). */
  async budgets(familyId: string, year: number, month: number): Promise<ExpenseBudgetResponse> {
    const { data } = await apiClient.get<ExpenseBudgetResponse>(`/families/${familyId}/expense-budgets`, {
      params: { year, month },
    });
    return data;
  },

  /** Saves the complete set of budgets from the given month on. */
  async setBudgets(familyId: string, payload: SetExpenseBudgetsRequest): Promise<ExpenseBudgetResponse> {
    const { data } = await apiClient.put<ExpenseBudgetResponse>(`/families/${familyId}/expense-budgets`, payload);
    return data;
  },
};
