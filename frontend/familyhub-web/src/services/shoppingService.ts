import apiClient from '../api/client';
import type {
  ShoppingItemRequest,
  ShoppingItemResponse,
  ShoppingListRequest,
  ShoppingListResponse,
} from '../types';

export const shoppingService = {
  async getLists(familyId: string): Promise<ShoppingListResponse[]> {
    const { data } = await apiClient.get<ShoppingListResponse[]>(
      `/families/${familyId}/shopping-lists`,
    );
    return data;
  },

  async createList(familyId: string, payload: ShoppingListRequest): Promise<ShoppingListResponse> {
    const { data } = await apiClient.post<ShoppingListResponse>(
      `/families/${familyId}/shopping-lists`,
      payload,
    );
    return data;
  },

  async addItem(listId: string, payload: ShoppingItemRequest): Promise<ShoppingItemResponse> {
    const { data } = await apiClient.post<ShoppingItemResponse>(
      `/shopping-lists/${listId}/items`,
      payload,
    );
    return data;
  },

  async toggleItem(listId: string, itemId: string): Promise<ShoppingItemResponse> {
    const { data } = await apiClient.patch<ShoppingItemResponse>(
      `/shopping-lists/${listId}/items/${itemId}/toggle`,
    );
    return data;
  },

  async deleteItem(listId: string, itemId: string): Promise<void> {
    await apiClient.delete(`/shopping-lists/${listId}/items/${itemId}`);
  },

  async clearPurchased(listId: string): Promise<void> {
    await apiClient.delete(`/shopping-lists/${listId}/purchased-items`);
  },
};
