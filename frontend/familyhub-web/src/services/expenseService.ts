import apiClient from '../api/client';
import type {
  ExpenseCategoryResponse,
  ExpenseFilter,
  ExpenseRequest,
  ExpenseResponse,
  ExpenseSummaryResponse,
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
};
