export type ExpiryStatus = 'valid' | 'expiringSoon' | 'expired' | 'none';

export interface ExpiryInfo {
  status: ExpiryStatus;
  /** Whole days until the date; negative when already past. Null when there is no date. */
  days: number | null;
}

const DAY_MS = 24 * 60 * 60 * 1000;

/** Classifies a date as Valid / Expiring Soon / Expired and computes the remaining days. */
export function getExpiryInfo(value: unknown, warnDays = 30): ExpiryInfo {
  if (typeof value !== 'string' || value === '') {
    return { status: 'none', days: null };
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return { status: 'none', days: null };
  }

  const startOfToday = new Date();
  startOfToday.setHours(0, 0, 0, 0);
  const startOfTarget = new Date(date);
  startOfTarget.setHours(0, 0, 0, 0);

  const days = Math.round((startOfTarget.getTime() - startOfToday.getTime()) / DAY_MS);

  if (days < 0) {
    return { status: 'expired', days };
  }
  if (days <= warnDays) {
    return { status: 'expiringSoon', days };
  }
  return { status: 'valid', days };
}

export function expiryChipClasses(status: ExpiryStatus): string {
  switch (status) {
    case 'valid':
      return 'bg-green-50 text-green-700';
    case 'expiringSoon':
      return 'bg-amber-50 text-amber-700';
    case 'expired':
      return 'bg-red-50 text-red-700';
    default:
      return 'bg-gray-100 text-gray-500';
  }
}

const DOCUMENT_TYPE_KEYS = [
  'vault.documentTypes.identityCard',
  'vault.documentTypes.passport',
  'vault.documentTypes.drivingLicense',
  'vault.documentTypes.healthCard',
  'vault.documentTypes.residencePermit',
  'vault.documentTypes.studentDocument',
];

export function documentTypeKey(type: number): string {
  return DOCUMENT_TYPE_KEYS[type] ?? 'vault.documentTypes.identityCard';
}
