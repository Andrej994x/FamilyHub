import { describe, expect, it } from 'vitest';
import i18n from '../locales/i18n';
import en from '../locales/en/translation.json';
import mk from '../locales/mk/translation.json';
import { DefaultExpenseCategory } from '../types';
import { categoryColor, categoryLabel, usageColor } from './expenseCategories';

describe('expense category helpers', () => {
  it('keeps the category enum in sync with the backend DefaultExpenseCategory order', () => {
    expect(DefaultExpenseCategory).toEqual({
      FoodGroceries: 0,
      BillsUtilities: 1,
      Transport: 2,
      KidsEducation: 3,
      Health: 4,
      Shopping: 5,
      Entertainment: 6,
      Home: 7,
      Other: 8,
    });
  });

  it('translates built-in categories and keeps custom names as typed', async () => {
    const t = i18n.t.bind(i18n);
    expect(categoryLabel('Food & Groceries', DefaultExpenseCategory.FoodGroceries, t)).toBe(en.expenses.defaultCategories.foodGroceries);
    expect(categoryLabel('Fuel', null, t)).toBe('Fuel');

    await i18n.changeLanguage('mk');
    expect(categoryLabel('Food & Groceries', DefaultExpenseCategory.FoodGroceries, t)).toBe(mk.expenses.defaultCategories.foodGroceries);
    expect(categoryLabel('Other', DefaultExpenseCategory.Other, t)).toBe(mk.expenses.defaultCategories.other);
    expect(categoryLabel('Гориво', null, t)).toBe('Гориво');
  });

  it('has a translation for every built-in category in both languages', () => {
    expect(Object.keys(en.expenses.defaultCategories)).toHaveLength(Object.keys(DefaultExpenseCategory).length);
    expect(Object.keys(mk.expenses.defaultCategories)).toEqual(Object.keys(en.expenses.defaultCategories));
  });

  it('colours budget usage by threshold and categories by sort order', () => {
    expect(usageColor(79)).toBe('bg-brand-500');
    expect(usageColor(80)).toBe('bg-amber-500');
    expect(usageColor(100)).toBe('bg-amber-500');
    expect(usageColor(101)).toBe('bg-red-500');
    expect(categoryColor(0)).toBe(categoryColor(9));
    expect(categoryColor(0)).not.toBe(categoryColor(1));
  });
});
