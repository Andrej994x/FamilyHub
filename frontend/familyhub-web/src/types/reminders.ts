/** Category names exactly as the backend enum serialises them (used in the API route). */
export const REMINDER_CATEGORIES = [
  'Tasks',
  'CalendarEvents',
  'Documents',
  'Vehicles',
  'Pets',
  'Home',
  'Warranties',
  'OtherVaultItems',
] as const;

export type ReminderCategory = (typeof REMINDER_CATEGORIES)[number];

/** The four standard lead times the UI exposes (backend also accepts custom values). */
export const STANDARD_OFFSETS = [30, 7, 1, 0] as const;

export interface ReminderPreference {
  category: string;
  isEnabled: boolean;
  reminderOffsetsDays: number[];
  /** True when the user has no saved row and these are the system defaults. */
  isDefault: boolean;
}

export interface UpdateReminderPreferenceRequest {
  isEnabled: boolean;
  reminderOffsetsDays: number[];
}
