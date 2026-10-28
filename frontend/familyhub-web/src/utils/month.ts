/** A calendar month; `month` is 1-12 like the API. */
export interface YearMonth {
  year: number;
  month: number;
}

/** The current month in the user's local time. */
export function currentMonth(now: Date = new Date()): YearMonth {
  return { year: now.getFullYear(), month: now.getMonth() + 1 };
}

export function addMonths(value: YearMonth, months: number): YearMonth {
  const index = value.year * 12 + (value.month - 1) + months;
  return { year: Math.floor(index / 12), month: (index % 12) + 1 };
}

export function compareMonths(a: YearMonth, b: YearMonth): number {
  return a.year * 12 + a.month - (b.year * 12 + b.month);
}

/** "October 2026" / "Октомври 2026 г." (capitalized, since Macedonian month names are lowercase). */
export function formatMonthYear(value: YearMonth, locale: string): string {
  const label = new Date(value.year, value.month - 1, 1).toLocaleDateString(locale, {
    month: 'long',
    year: 'numeric',
  });
  return label.charAt(0).toLocaleUpperCase(locale) + label.slice(1);
}

/** A local Date as a yyyy-MM-dd calendar date (not shifted to UTC). */
export function toDateOnly(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** Today's local calendar date as yyyy-MM-dd. */
export function todayDateOnly(now: Date = new Date()): string {
  return toDateOnly(now);
}

/**
 * Formats a yyyy-MM-dd calendar date. Parsed as a local date, so it never moves to the previous
 * day the way `new Date('2026-10-01')` (UTC midnight) does west of UTC.
 */
export function formatDateOnly(value: string, locale: string): string {
  const [year, month, day] = value.split('-').map(Number);
  if (!year || !month || !day) {
    return '';
  }
  return new Date(year, month - 1, day).toLocaleDateString(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}
