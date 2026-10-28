import { describe, expect, it } from 'vitest';
import { addMonths, compareMonths, currentMonth, formatDateOnly, formatMonthYear, todayDateOnly, toDateOnly } from './month';

describe('month helpers', () => {
  it('uses 1-based months like the API', () => {
    expect(currentMonth(new Date(2026, 0, 31))).toEqual({ year: 2026, month: 1 });
    expect(currentMonth(new Date(2026, 11, 1))).toEqual({ year: 2026, month: 12 });
  });

  it('adds and compares months across year boundaries', () => {
    expect(addMonths({ year: 2026, month: 1 }, -1)).toEqual({ year: 2025, month: 12 });
    expect(addMonths({ year: 2026, month: 12 }, 1)).toEqual({ year: 2027, month: 1 });
    expect(addMonths({ year: 2026, month: 10 }, -22)).toEqual({ year: 2024, month: 12 });
    expect(compareMonths({ year: 2026, month: 9 }, { year: 2026, month: 10 })).toBeLessThan(0);
    expect(compareMonths({ year: 2027, month: 1 }, { year: 2026, month: 12 })).toBeGreaterThan(0);
    expect(compareMonths({ year: 2026, month: 10 }, { year: 2026, month: 10 })).toBe(0);
  });

  it('labels months in English and capitalized Macedonian', () => {
    expect(formatMonthYear({ year: 2026, month: 10 }, 'en')).toBe('October 2026');
    expect(formatMonthYear({ year: 2026, month: 10 }, 'mk')).toMatch(/^Октомври 2026/);
  });

  it('keeps calendar dates local instead of shifting them through UTC', () => {
    expect(toDateOnly(new Date(2026, 0, 5, 23, 59))).toBe('2026-01-05');
    expect(todayDateOnly(new Date(2026, 9, 1, 0, 30))).toBe('2026-10-01');
    // A yyyy-MM-dd date is shown as that day in any time zone.
    expect(formatDateOnly('2026-10-01', 'en')).toBe(new Date(2026, 9, 1).toLocaleDateString('en', { year: 'numeric', month: 'short', day: 'numeric' }));
    expect(formatDateOnly('not-a-date', 'en')).toBe('');
  });
});
