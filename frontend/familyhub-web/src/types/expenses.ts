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

/**
 * The budgets in effect for a month (MKD). A budget applies from the month it was set until it is
 * changed. `overallAmount` is null without an overall budget; unbudgeted categories are not listed.
 */
export interface ExpenseBudgetResponse {
  year: number;
  month: number;
  overallAmount: number | null;
  categories: { categoryId: string; amount: number }[];
}

/** The complete set of budgets from a month on; a category left out loses its budget from then. */
export interface SetExpenseBudgetsRequest {
  year: number;
  month: number;
  overallAmount: number | null;
  categories: { categoryId: string; amount: number | null }[];
}

export interface ExpenseCategoryRequest {
  name: string;
}

export interface UpdateExpenseCategoryRequest {
  name: string;
  isArchived: boolean;
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
