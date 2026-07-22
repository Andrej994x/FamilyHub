import apiClient from '../api/client';
import type { ShoppingListResponse } from '../types';

export const shoppingService = {
  async getLists(familyId: string): Promise<ShoppingListResponse[]> {
    const { data } = await apiClient.get<ShoppingListResponse[]>(
      `/families/${familyId}/shopping-lists`,
    );
    return data;
  },
};
