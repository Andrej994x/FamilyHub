import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useFamily } from '../hooks/useFamily';
import { eventService } from '../services/eventService';
import { familyService } from '../services/familyService';
import { childService } from '../services/childService';
import { getApiErrorMessage } from '../utils/apiError';
import {
  addDays,
  dayKey,
  formatDayLong,
  formatTime,
  isSameDay,
  startOfWeek,
  weekDays,
  weekRange,
} from '../utils/date';
import { eventTypeClasses, eventTypeKey } from '../utils/labels';
import { EventDialog } from '../components/EventDialog';
import { EventType } from '../types';
import type { ChildResponse, EventResponse, FamilyMemberResponse } from '../types';

const EVENT_TYPE_VALUES = [
  EventType.Kindergarten,
  EventType.School,
  EventType.Doctor,
  EventType.Training,
  EventType.Birthday,
  EventType.Family,
  EventType.Other,
];

export default function Calendar() {
  const { t, i18n } = useTranslation();
  const { family } = useFamily();
  const familyId = family?.id ?? '';

  const [weekStart, setWeekStart] = useState<Date>(() => startOfWeek(new Date()));
  const [selectedDay, setSelectedDay] = useState<Date | null>(null);
  const [typeFilters, setTypeFilters] = useState<Set<number>>(new Set());

  const [events, setEvents] = useState<EventResponse[]>([]);
  const [members, setMembers] = useState<FamilyMemberResponse[]>([]);
  const [children, setChildren] = useState<ChildResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingEvent, setEditingEvent] = useState<EventResponse | null>(null);

  const days = useMemo(() => weekDays(weekStart), [weekStart]);

  const loadEvents = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setLoading(true);
    setError(null);
    const { from, to } = weekRange(weekStart);
    try {
      setEvents(await eventService.list(familyId, { dateFrom: from, dateTo: to }));
    } catch (err) {
      setError(getApiErrorMessage(err, t('calendar.errors.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [familyId, weekStart, t]);

  useEffect(() => {
    void loadEvents();
  }, [loadEvents]);

  // Members and children are needed for the event dialog and row metadata.
  useEffect(() => {
    if (!familyId) {
      return;
    }
    familyService.getMembers(familyId).then(setMembers).catch(() => setMembers([]));
    childService.list(familyId).then(setChildren).catch(() => setChildren([]));
  }, [familyId]);

  const memberName = useCallback(
    (id: string | null): string | null => {
      if (!id) return null;
      const m = members.find((member) => member.id === id);
      return m ? `${m.firstName} ${m.lastName}` : null;
    },
    [members],
  );

  const childName = useCallback(
    (id: string | null): string | null => {
      if (!id) return null;
      const c = children.find((child) => child.id === id);
      return c ? `${c.firstName} ${c.lastName}` : null;
    },
    [children],
  );

  const matchesType = useCallback(
    (event: EventResponse) => typeFilters.size === 0 || typeFilters.has(event.eventType),
    [typeFilters],
  );

  // Days (within the week) that have at least one type-matching event — for strip indicators.
  const daysWithEvents = useMemo(() => {
    const set = new Set<string>();
    for (const event of events) {
      if (matchesType(event)) {
        set.add(dayKey(event.startDateTime));
      }
    }
    return set;
  }, [events, matchesType]);

  // Visible events grouped by calendar day, groups and events both sorted ascending.
  const groups = useMemo(() => {
    const visible = events
      .filter(matchesType)
      .filter((event) => !selectedDay || isSameDay(new Date(event.startDateTime), selectedDay));

    const map = new Map<string, EventResponse[]>();
    for (const event of visible) {
      const key = dayKey(event.startDateTime);
      const list = map.get(key) ?? [];
      list.push(event);
      map.set(key, list);
    }

    return [...map.entries()]
      .map(([key, items]) => ({
        key,
        items: items.sort((a, b) => a.startDateTime.localeCompare(b.startDateTime)),
      }))
      .sort((a, b) => a.key.localeCompare(b.key));
  }, [events, matchesType, selectedDay]);

  const toggleType = (type: number) => {
    setTypeFilters((prev) => {
      const next = new Set(prev);
      if (next.has(type)) {
        next.delete(type);
      } else {
        next.add(type);
      }
      return next;
    });
  };

  const goToday = () => {
    setWeekStart(startOfWeek(new Date()));
    setSelectedDay(null);
  };

  const shiftWeek = (delta: number) => {
    setWeekStart((prev) => addDays(prev, delta * 7));
    setSelectedDay(null);
  };

  const selectDay = (day: Date) => {
    setSelectedDay((prev) => (prev && isSameDay(prev, day) ? null : day));
  };

  const openCreate = () => {
    setEditingEvent(null);
    setDialogOpen(true);
  };

  const openEdit = (event: EventResponse) => {
    setEditingEvent(event);
    setDialogOpen(true);
  };

  const handleDelete = async (event: EventResponse) => {
    if (!window.confirm(t('calendar.deleteConfirm'))) {
      return;
    }
    try {
      await eventService.remove(familyId, event.id);
      await loadEvents();
    } catch (err) {
      setError(getApiErrorMessage(err, t('calendar.errors.deleteFailed')));
    }
  };

  const weekLabel = `${days[0].toLocaleDateString(i18n.language, { month: 'short', day: 'numeric' })} – ${days[6].toLocaleDateString(i18n.language, { month: 'short', day: 'numeric' })}`;

  const hasActiveFilters = typeFilters.size > 0 || selectedDay !== null;

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-4xl">
      <header className="mb-6 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('nav.calendar')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('pages.calendar.subtitle')}</p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
        >
          {t('calendar.add')}
        </button>
      </header>

      {/* Week navigation */}
      <div className="mb-3 flex items-center justify-between gap-3">
        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => shiftWeek(-1)}
            aria-label={t('calendar.prevWeek')}
            className="rounded-lg p-2 text-gray-500 hover:bg-gray-100"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
            </svg>
          </button>
          <span className="min-w-32 text-center text-sm font-medium text-gray-900">{weekLabel}</span>
          <button
            type="button"
            onClick={() => shiftWeek(1)}
            aria-label={t('calendar.nextWeek')}
            className="rounded-lg p-2 text-gray-500 hover:bg-gray-100"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
            </svg>
          </button>
        </div>
        <button
          type="button"
          onClick={goToday}
          className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
        >
          {t('calendar.today')}
        </button>
      </div>

      {/* Weekly date selector */}
      <div className="mb-5 grid grid-cols-7 gap-1.5">
        {days.map((day) => {
          const today = isSameDay(day, new Date());
          const active = selectedDay !== null && isSameDay(day, selectedDay);
          const hasEvents = daysWithEvents.has(dayKey(day));
          return (
            <button
              key={day.toISOString()}
              type="button"
              onClick={() => selectDay(day)}
              className={`flex flex-col items-center gap-1 rounded-xl border py-2 text-sm transition ${
                active
                  ? 'border-brand-600 bg-brand-600 text-white'
                  : today
                    ? 'border-brand-200 bg-brand-50 text-brand-700'
                    : 'border-gray-200 bg-white text-gray-700 hover:bg-gray-50'
              }`}
            >
              <span className="text-[0.65rem] font-medium uppercase">
                {day.toLocaleDateString(i18n.language, { weekday: 'short' })}
              </span>
              <span className="text-base font-semibold">{day.getDate()}</span>
              <span
                className={`h-1.5 w-1.5 rounded-full ${
                  hasEvents ? (active ? 'bg-white' : 'bg-brand-500') : 'bg-transparent'
                }`}
              />
            </button>
          );
        })}
      </div>

      {/* Event type filters */}
      <div className="mb-5 flex flex-wrap gap-2">
        {EVENT_TYPE_VALUES.map((type) => {
          const active = typeFilters.has(type);
          return (
            <button
              key={type}
              type="button"
              onClick={() => toggleType(type)}
              className={`rounded-full px-3 py-1 text-xs font-medium transition ${
                active
                  ? `${eventTypeClasses(type)} ring-2 ring-brand-300`
                  : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
              }`}
            >
              {t(eventTypeKey(type))}
            </button>
          );
        })}
        {hasActiveFilters && (
          <button
            type="button"
            onClick={() => {
              setTypeFilters(new Set());
              setSelectedDay(null);
            }}
            className="px-2 py-1 text-xs font-medium text-brand-600 hover:underline"
          >
            {t('calendar.filters.clear')}
          </button>
        )}
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {loading ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : groups.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {hasActiveFilters ? t('calendar.emptyFiltered') : t('calendar.empty')}
        </div>
      ) : (
        <div className="space-y-6">
          {groups.map((group) => (
            <section key={group.key}>
              <h2 className="mb-2 text-sm font-semibold text-gray-900">
                {formatDayLong(group.items[0].startDateTime, i18n.language)}
              </h2>
              <div className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
                {group.items.map((event) => {
                  const assignee = memberName(event.assignedMemberId);
                  const child = childName(event.childProfileId);
                  return (
                    <div key={event.id} className="flex items-start gap-3 p-4">
                      <span className="mt-0.5 w-14 shrink-0 text-xs font-medium text-gray-500">
                        {formatTime(event.startDateTime, i18n.language)}
                      </span>
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <p className="font-medium text-gray-900">{event.title}</p>
                          <span
                            className={`rounded-full px-2 py-0.5 text-xs font-medium ${eventTypeClasses(event.eventType)}`}
                          >
                            {t(eventTypeKey(event.eventType))}
                          </span>
                        </div>
                        {event.description && (
                          <p className="mt-0.5 line-clamp-2 text-sm text-gray-500">
                            {event.description}
                          </p>
                        )}
                        <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-gray-400">
                          {event.endDateTime && (
                            <span>
                              {t('calendar.until', {
                                time: formatTime(event.endDateTime, i18n.language),
                              })}
                            </span>
                          )}
                          {event.location && <span>{event.location}</span>}
                          {assignee && <span>{assignee}</span>}
                          {child && <span>{child}</span>}
                        </div>
                      </div>
                      <div className="flex shrink-0 items-center gap-3">
                        <button
                          type="button"
                          onClick={() => openEdit(event)}
                          className="text-xs font-medium text-brand-600 hover:underline"
                        >
                          {t('common.edit')}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(event)}
                          className="text-xs font-medium text-red-600 hover:underline"
                        >
                          {t('common.delete')}
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </section>
          ))}
        </div>
      )}

      <EventDialog
        open={dialogOpen}
        familyId={familyId}
        event={editingEvent}
        members={members}
        childProfiles={children}
        defaultDate={selectedDay}
        onClose={() => setDialogOpen(false)}
        onSaved={loadEvents}
      />
    </div>
  );
}
