import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { reminderService } from '../services/reminderService';
import { getApiErrorMessage } from '../utils/apiError';
import { STANDARD_OFFSETS, type ReminderPreference } from '../types/reminders';

interface Row {
  category: string;
  isEnabled: boolean;
  offsets: number[]; // subset of STANDARD_OFFSETS
  isDefault: boolean;
  saving: boolean;
  error: string | null;
}

const STANDARD = STANDARD_OFFSETS as readonly number[];

function toRow(pref: ReminderPreference): Row {
  return {
    category: pref.category,
    isEnabled: pref.isEnabled,
    offsets: pref.reminderOffsetsDays.filter((o) => STANDARD.includes(o)),
    isDefault: pref.isDefault,
    saving: false,
    error: null,
  };
}

function categoryKey(category: string): string {
  return category.charAt(0).toLowerCase() + category.slice(1);
}

function offsetKey(offset: number): string {
  return offset === 0 ? 'd0' : `d${offset}`;
}

export default function ReminderSettings() {
  const { t } = useTranslation();

  const [rows, setRows] = useState<Row[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [resetting, setResetting] = useState(false);
  const [expanded, setExpanded] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const prefs = await reminderService.getAll();
      setRows(prefs.map(toRow));
    } catch (err) {
      setError(getApiErrorMessage(err, t('reminders.loadError')));
    } finally {
      setLoading(false);
    }
  }, [t]);

  useEffect(() => {
    void load();
  }, [load]);

  const patchRow = (category: string, patch: Partial<Row>) =>
    setRows((prev) => prev.map((r) => (r.category === category ? { ...r, ...patch } : r)));

  /** Optimistically apply + persist a category; reverts on failure. */
  const persist = async (row: Row, next: { isEnabled: boolean; offsets: number[] }) => {
    patchRow(row.category, { ...next, saving: true, error: null });
    try {
      const updated = await reminderService.update(row.category, {
        isEnabled: next.isEnabled,
        reminderOffsetsDays: [...next.offsets].sort((a, b) => b - a),
      });
      patchRow(row.category, {
        isEnabled: updated.isEnabled,
        offsets: updated.reminderOffsetsDays.filter((o) => STANDARD.includes(o)),
        isDefault: updated.isDefault,
        saving: false,
      });
    } catch (err) {
      // Revert to the pre-change values.
      patchRow(row.category, {
        isEnabled: row.isEnabled,
        offsets: row.offsets,
        saving: false,
        error: getApiErrorMessage(err, t('reminders.saveError')),
      });
    }
  };

  const toggleEnabled = (row: Row) => void persist(row, { isEnabled: !row.isEnabled, offsets: row.offsets });

  const toggleOffset = (row: Row, offset: number) => {
    const has = row.offsets.includes(offset);
    // Always keep at least one lead time (turn the category off instead of clearing all).
    if (has && row.offsets.length === 1) {
      patchRow(row.category, { error: t('reminders.atLeastOne') });
      return;
    }
    const offsets = has ? row.offsets.filter((o) => o !== offset) : [...row.offsets, offset];
    void persist(row, { isEnabled: row.isEnabled, offsets });
  };

  const resetDefaults = async () => {
    if (!window.confirm(t('reminders.resetConfirm'))) {
      return;
    }
    setResetting(true);
    try {
      const prefs = await reminderService.resetDefaults();
      setRows(prefs.map(toRow));
      setExpanded(null);
    } catch (err) {
      setError(getApiErrorMessage(err, t('reminders.saveError')));
    } finally {
      setResetting(false);
    }
  };

  const summary = (row: Row): string => {
    if (!row.isEnabled) {
      return t('reminders.off');
    }
    return [...row.offsets]
      .sort((a, b) => b - a)
      .map((o) => (o === 0 ? t('reminders.sameDayShort') : `${o}${t('reminders.dayUnit')}`))
      .join(' · ');
  };

  return (
    <div className="mx-auto max-w-2xl">
      <header className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('reminders.title')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('reminders.subtitle')}</p>
        </div>
        {rows.length > 0 && (
          <button
            type="button"
            onClick={() => void resetDefaults()}
            disabled={resetting}
            className="shrink-0 rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-60"
          >
            {t('reminders.reset')}
          </button>
        )}
      </header>

      {loading ? (
        <div className="rounded-xl border border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {t('common.loading')}
        </div>
      ) : error ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-gray-200 bg-white p-10 text-center">
          <p className="text-sm text-gray-500">{error}</p>
          <button
            type="button"
            onClick={() => void load()}
            className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700"
          >
            {t('common.retry')}
          </button>
        </div>
      ) : (
        <div className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
          {rows.map((row) => {
            const isOpen = expanded === row.category;
            return (
              <div key={row.category}>
                {/* Compact header row */}
                <div className="flex items-center gap-2 px-3 py-2.5 sm:px-4">
                  <button
                    type="button"
                    onClick={() => setExpanded(isOpen ? null : row.category)}
                    aria-expanded={isOpen}
                    className="flex min-w-0 flex-1 items-center gap-2 text-left"
                  >
                    <svg
                      className={`h-4 w-4 shrink-0 text-gray-400 transition-transform ${isOpen ? 'rotate-90' : ''}`}
                      fill="none"
                      viewBox="0 0 24 24"
                      strokeWidth={2}
                      stroke="currentColor"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                    </svg>
                    <span className="min-w-0">
                      <span className="block text-sm font-semibold text-gray-900">
                        {t(`reminders.categories.${categoryKey(row.category)}`)}
                      </span>
                      <span
                        className={`block truncate text-xs ${row.isEnabled ? 'text-gray-500' : 'text-gray-400'}`}
                      >
                        {summary(row)}
                      </span>
                    </span>
                  </button>

                  {row.saving && <span className="shrink-0 text-xs text-gray-400">{t('reminders.saving')}</span>}

                  <button
                    type="button"
                    role="switch"
                    aria-checked={row.isEnabled}
                    aria-label={t(`reminders.categories.${categoryKey(row.category)}`)}
                    onClick={() => toggleEnabled(row)}
                    className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors ${
                      row.isEnabled ? 'bg-brand-600' : 'bg-gray-300'
                    }`}
                  >
                    <span
                      className={`inline-block h-5 w-5 transform rounded-full bg-white shadow transition-transform ${
                        row.isEnabled ? 'translate-x-5' : 'translate-x-0.5'
                      }`}
                    />
                  </button>
                </div>

                {/* Expandable lead-time picker */}
                {isOpen && (
                  <div className="border-t border-gray-100 bg-gray-50/50 px-3 py-3 sm:px-4">
                    <div className="flex flex-wrap gap-2">
                      {STANDARD_OFFSETS.map((offset) => {
                        const checked = row.offsets.includes(offset);
                        return (
                          <button
                            key={offset}
                            type="button"
                            aria-pressed={checked}
                            onClick={() => toggleOffset(row, offset)}
                            className={`rounded-full border px-3 py-1.5 text-xs font-medium transition-colors ${
                              checked
                                ? 'border-brand-600 bg-brand-50 text-brand-700'
                                : 'border-gray-300 bg-white text-gray-600 hover:bg-gray-100'
                            }`}
                          >
                            {t(`reminders.offset.${offsetKey(offset)}`)}
                          </button>
                        );
                      })}
                    </div>
                    {row.error && <p className="mt-2 text-xs text-red-600">{row.error}</p>}
                    {row.isDefault && !row.error && (
                      <p className="mt-2 text-xs text-gray-400">{t('reminders.usingDefaults')}</p>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
