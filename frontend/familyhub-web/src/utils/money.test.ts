import { describe, expect, it } from 'vitest';
import { formatMoney, parseAmount } from './money';

describe('formatMoney', () => {
  it('formats MKD for English and Macedonian, without decimals for whole amounts', () => {
    // Compare with Intl itself so the test doesn't depend on its exact spacing characters.
    const expected = (locale: string, value: number, digits: number) =>
      new Intl.NumberFormat(locale, {
        style: 'currency',
        currency: 'MKD',
        minimumFractionDigits: digits,
        maximumFractionDigits: 2,
      }).format(value);

    expect(formatMoney(32000, 'en')).toBe(expected('en', 32000, 0));
    expect(formatMoney(1234.5, 'en')).toBe(expected('en', 1234.5, 2));
    expect(formatMoney(32000, 'mk')).toBe(expected('mk', 32000, 0));
    expect(formatMoney(32000, 'en')).toContain('MKD');
    expect(formatMoney(32000, 'mk')).toContain('ден');
    expect(formatMoney(1234.5, 'mk')).toMatch(/1\.234,50/);
  });
});

describe('parseAmount', () => {
  it.each([
    ['1500', 1500],
    ['1500.50', 1500.5],
    ['1500,50', 1500.5],
    [' 1 500 ', 1500],
    ['0.01', 0.01],
    ['7,5', 7.5],
  ])('parses %j as %d', (input, expected) => {
    expect(parseAmount(input)).toBe(expected);
  });

  it.each(['', 'abc', '12.345', '1.500,50', '1,500.50', '-5', '1e3', '12.', '.5'])(
    'rejects %j instead of guessing',
    (input) => {
      expect(parseAmount(input)).toBeNull();
    },
  );
});
