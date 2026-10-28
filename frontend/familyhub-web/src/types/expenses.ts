/** Built-in expense categories; integer order matches the backend DefaultExpenseCategory enum. */
export const DefaultExpenseCategory = {
  FoodGroceries: 0,
  BillsUtilities: 1,
  Transport: 2,
  KidsEducation: 3,
  Health: 4,
  Shopping: 5,
  Entertainment: 6,
  Home: 7,
  Other: 8,
} as const;

export interface ExpenseCategoryResponse {
  id: string;
  name: string;
  /** Set for built-in categories; the UI shows a translated label instead of `name`. */
  defaultCategory: number | null;
  isArchived: boolean;
  sortOrder: number;
}

/** An expense. Amounts are MKD; `date` is a calendar date (yyyy-MM-dd). */
export interface ExpenseResponse {
  id: string;
  familyId: string;
  amount: number;
  date: string;
  categoryId: string;
  categoryName: string;
  categoryDefault: number | null;
  categoryIsArchived: boolean;
  paidByUserId: string;
  paidByName: string;
  description: string | null;
  createdByUserId: string;
  createdByName: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface ExpenseRequest {
  amount: number;
  categoryId: string;
  date: string;
  paidByUserId: string;
  description: string | null;
}

/** History filters; the month is always required by the API. */
export interface ExpenseFilter {
  year: number;
  month: number;
  categoryId?: string;
  paidByUserId?: string;
}

export interface CategorySpendingResponse {
  categoryId: string;
  name: string;
  defaultCategory: number | null;
  isArchived: boolean;
  spent: number;
  expenseCount: number;
  budget: number | null;
  remaining: number | null;
  usagePercent: number | null;
}

/** One month's dashboard numbers. Budget fields are null when no budget is in effect. */
export interface ExpenseSummaryResponse {
  year: number;
  month: number;
  totalSpent: number;
  expenseCount: number;
  budget: number | null;
  remaining: number | null;
  usagePercent: number | null;
  categories: CategorySpendingResponse[];
  recentExpenses: ExpenseResponse[];
}
