import type { TFunction } from 'i18next';
import { DefaultExpenseCategory } from '../types';

const DEFAULT_KEYS: Record<number, string> = {
  [DefaultExpenseCategory.FoodGroceries]: 'foodGroceries',
  [DefaultExpenseCategory.BillsUtilities]: 'billsUtilities',
  [DefaultExpenseCategory.Transport]: 'transport',
  [DefaultExpenseCategory.KidsEducation]: 'kidsEducation',
  [DefaultExpenseCategory.Health]: 'health',
  [DefaultExpenseCategory.Shopping]: 'shopping',
  [DefaultExpenseCategory.Entertainment]: 'entertainment',
  [DefaultExpenseCategory.Home]: 'home',
  [DefaultExpenseCategory.Other]: 'other',
};

/**
 * The label for a category: built-in categories are translated, custom (or renamed) ones show
 * the name the family typed.
 */
export function categoryLabel(name: string, defaultCategory: number | null, t: TFunction): string {
  const key = defaultCategory === null ? undefined : DEFAULT_KEYS[defaultCategory];
  return key ? t(`expenses.defaultCategories.${key}`) : name;
}

const BAR_COLORS = [
  'bg-brand-500',
  'bg-emerald-500',
  'bg-amber-500',
  'bg-sky-500',
  'bg-rose-500',
  'bg-violet-500',
  'bg-teal-500',
  'bg-orange-500',
  'bg-slate-400',
];

/** A stable bar colour for a category, from its sort order. */
export function categoryColor(sortOrder: number): string {
  return BAR_COLORS[Math.abs(sortOrder) % BAR_COLORS.length];
}

/** Progress colour: brand below 80 %, amber up to 100 %, red when over budget. */
export function usageColor(percent: number): string {
  if (percent > 100) return 'bg-red-500';
  if (percent >= 80) return 'bg-amber-500';
  return 'bg-brand-500';
}
