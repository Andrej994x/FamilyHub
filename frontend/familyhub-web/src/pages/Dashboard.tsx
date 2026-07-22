import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { useFamily } from '../hooks/useFamily';
import { eventService } from '../services/eventService';
import { taskService } from '../services/taskService';
import { pickupService } from '../services/pickupService';
import { shoppingService } from '../services/shoppingService';
import { notificationService } from '../services/notificationService';
import { getApiErrorMessage } from '../utils/apiError';
import { formatTime, formatTodayLong, isToday, todayRange } from '../utils/date';
import {
  eventTypeKey,
  pickupStatusClasses,
  pickupStatusKey,
  priorityClasses,
  priorityKey,
} from '../utils/labels';
import { TaskStatus } from '../types';
import type {
  EventResponse,
  PickupResponse,
  ShoppingListResponse,
  TaskResponse,
} from '../types';
import { Card } from '../components/dashboard/Card';
import { EmptyState } from '../components/dashboard/EmptyState';
import { StatTile } from '../components/dashboard/StatTile';
import { QuickActions } from '../components/dashboard/QuickActions';

interface DashboardData {
  events: EventResponse[];
  tasks: TaskResponse[];
  pickups: PickupResponse[];
  lists: ShoppingListResponse[];
  unread: number;
}

export default function Dashboard() {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const { family } = useFamily();
  const navigate = useNavigate();
  const familyId = family?.id ?? '';

  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setLoading(true);
    setError(null);

    const { from, to } = todayRange();

    try {
      const [events, tasks, pickups, lists, unread] = await Promise.all([
        eventService.list(familyId, { dateFrom: from, dateTo: to }),
        taskService.list(familyId, { status: TaskStatus.Pending, dueFrom: from, dueTo: to }),
        pickupService.list(familyId),
        shoppingService.getLists(familyId),
        notificationService.getUnreadCount(),
      ]);

      setData({
        events,
        tasks,
        pickups: pickups.filter((pickup) => isToday(pickup.pickupDateTime)),
        lists,
        unread,
      });
    } catch (err) {
      setError(getApiErrorMessage(err, t('dashboard.loadError')));
    } finally {
      setLoading(false);
    }
  }, [familyId, t]);

  useEffect(() => {
    void load();
  }, [load]);

  const shoppingSummary = useMemo(() => {
    const lists = data?.lists ?? [];
    const remaining = lists.reduce(
      (total, list) => total + list.items.filter((item) => !item.isPurchased).length,
      0,
    );
    return { listCount: lists.length, remaining };
  }, [data]);

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      {/* Greeting */}
      <header>
        <h1 className="text-2xl font-semibold text-gray-900">
          {t('dashboard.greeting', { name: user?.firstName ?? '' })}
        </h1>
        <p className="mt-1 text-sm text-gray-500">{formatTodayLong(i18n.language)}</p>
      </header>

      {/* Quick actions */}
      <QuickActions />

      {error && (
        <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {loading ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : (
        <>
          {/* Stat tiles */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <StatTile
              label={t('dashboard.stats.notifications')}
              value={data?.unread ?? 0}
              hint={t('dashboard.stats.notificationsHint')}
              icon={
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
                </svg>
              }
            />
            <StatTile
              label={t('dashboard.stats.shopping')}
              value={shoppingSummary.remaining}
              hint={t('dashboard.shopping.summary', {
                lists: shoppingSummary.listCount,
                items: shoppingSummary.remaining,
              })}
              onClick={() => navigate('/shopping')}
              icon={
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M4.5 5.25h15l-1.5 8.25H6L4.5 5.25z" />
                </svg>
              }
            />
          </div>

          {/* Content cards */}
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            {/* Today's events */}
            <Card title={t('dashboard.sections.events')}>
              {data && data.events.length > 0 ? (
                <ul className="space-y-3">
                  {data.events.map((event) => (
                    <li key={event.id} className="flex items-start gap-3">
                      <span className="mt-0.5 w-12 shrink-0 text-xs font-medium text-gray-500">
                        {formatTime(event.startDateTime, i18n.language)}
                      </span>
                      <div className="min-w-0">
                        <p className="truncate text-sm font-medium text-gray-900">{event.title}</p>
                        <p className="text-xs text-gray-400">{t(eventTypeKey(event.eventType))}</p>
                      </div>
                    </li>
                  ))}
                </ul>
              ) : (
                <EmptyState message={t('dashboard.empty.events')} />
              )}
            </Card>

            {/* Today's pending tasks */}
            <Card title={t('dashboard.sections.tasks')}>
              {data && data.tasks.length > 0 ? (
                <ul className="space-y-3">
                  {data.tasks.map((task) => (
                    <li key={task.id} className="flex items-center justify-between gap-3">
                      <p className="min-w-0 truncate text-sm font-medium text-gray-900">
                        {task.title}
                      </p>
                      <span
                        className={`shrink-0 rounded-full px-2 py-0.5 text-xs font-medium ${priorityClasses(task.priority)}`}
                      >
                        {t(priorityKey(task.priority))}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : (
                <EmptyState message={t('dashboard.empty.tasks')} />
              )}
            </Card>

            {/* Today's pickups */}
            <Card title={t('dashboard.sections.pickups')}>
              {data && data.pickups.length > 0 ? (
                <ul className="space-y-3">
                  {data.pickups.map((pickup) => (
                    <li key={pickup.id} className="flex items-start gap-3">
                      <span className="mt-0.5 w-12 shrink-0 text-xs font-medium text-gray-500">
                        {formatTime(pickup.pickupDateTime, i18n.language)}
                      </span>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium text-gray-900">
                          {pickup.location}
                        </p>
                        <span
                          className={`mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${pickupStatusClasses(pickup.status)}`}
                        >
                          {t(pickupStatusKey(pickup.status))}
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              ) : (
                <EmptyState message={t('dashboard.empty.pickups')} />
              )}
            </Card>
          </div>
        </>
      )}
    </div>
  );
}
