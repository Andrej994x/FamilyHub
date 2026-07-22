export const ItemCategory = {
  Grocery: 0,
  Pharmacy: 1,
  Home: 2,
  Child: 3,
  Other: 4,
} as const;

export interface ShoppingItemResponse {
  id: string;
  shoppingListId: string;
  name: string;
  quantity: string | null;
  category: number;
  isPurchased: boolean;
  addedByUserId: string;
  purchasedByUserId: string | null;
  createdAt: string;
  purchasedAt: string | null;
}

export interface ShoppingListResponse {
  id: string;
  familyId: string;
  name: string;
  createdAt: string;
  items: ShoppingItemResponse[];
}

export interface ShoppingListRequest {
  name: string;
}

export interface ShoppingItemRequest {
  name: string;
  quantity: string | null;
  category: number;
}
