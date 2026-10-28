/** The only currency in Family Expenses v1. */
export const CURRENCY = 'MKD';

/** The largest amount the API accepts for one expense. */
export const MAX_AMOUNT = 10_000_000;

/** Formats an MKD amount for the UI language: whole amounts without decimals, others with two. */
export function formatMoney(amount: number, locale: string): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: CURRENCY,
    minimumFractionDigits: Number.isInteger(amount) ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

/**
 * Parses an amount typed by the user. Accepts digits with an optional decimal part of at most two
 * digits after "." or "," (so both "1500.50" and "1500,50" work), and ignores spaces. Anything else,
 * including grouped input such as "1.500,50", returns null rather than guessing.
 */
export function parseAmount(input: string): number | null {
  const compact = input.replace(/\s/g, '');
  if (!/^\d+([.,]\d{1,2})?$/.test(compact)) {
    return null;
  }
  return Number(compact.replace(',', '.'));
}

/** An amount as the user would type it in an input: 1500, 1500.50, or 1500,50 in Macedonian. */
export function toAmountInput(amount: number, language: string): string {
  const text = Number.isInteger(amount) ? String(amount) : amount.toFixed(2);
  return language === 'mk' ? text.replace('.', ',') : text;
}
