import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getExpiryInfo } from './vaultStatus';

describe('getExpiryInfo', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 5, 15, 14, 30)); // 15 June 2026, local time
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it.each([
    [undefined, 'none', null],
    ['', 'none', null],
    ['not-a-date', 'none', null],
  ])('returns none for %j', (value, status, days) => {
    expect(getExpiryInfo(value)).toEqual({ status, days });
  });

  it('classifies past dates as expired with negative days', () => {
    // Local date-times (no "Z") keep the day arithmetic independent of the machine's time zone.
    expect(getExpiryInfo('2026-06-14T12:00:00')).toEqual({ status: 'expired', days: -1 });
  });

  it('treats today and dates within the warning window as expiring soon', () => {
    expect(getExpiryInfo('2026-06-15T23:00:00')).toEqual({ status: 'expiringSoon', days: 0 });
    expect(getExpiryInfo('2026-07-15T08:00:00')).toEqual({ status: 'expiringSoon', days: 30 });
  });

  it('classifies dates beyond the warning window as valid', () => {
    expect(getExpiryInfo('2026-07-16T08:00:00')).toEqual({ status: 'valid', days: 31 });
  });

  it('honours a custom warning window', () => {
    expect(getExpiryInfo('2026-06-25T08:00:00', 7)).toEqual({ status: 'valid', days: 10 });
  });
});
