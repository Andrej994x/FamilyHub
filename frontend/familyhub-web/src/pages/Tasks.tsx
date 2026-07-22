import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useFamily } from '../hooks/useFamily';
import { familyService } from '../services/familyService';
import { taskService } from '../services/taskService';
import { getApiErrorMessage } from '../utils/apiError';
import { formatDate } from '../utils/format';
import { priorityClasses, priorityKey, taskStatusClasses, taskStatusKey } from '../utils/labels';
import { TaskDialog } from '../components/TaskDialog';
import { TaskPriority, TaskStatus } from '../types';
import type { FamilyMemberResponse, TaskFilter, TaskResponse } from '../types';

const STATUS_VALUES = [
  TaskStatus.Pending,
  TaskStatus.InProgress,
  TaskStatus.Completed,
  TaskStatus.Cancelled,
];

const PRIORITY_VALUES = [TaskPriority.Low, TaskPriority.Medium, TaskPriority.High];

const selectClass =
  'rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

export default function Tasks() {
  const { t, i18n } = useTranslation();
  const { family } = useFamily();
  const familyId = family?.id ?? '';

  const [tasks, setTasks] = useState<TaskResponse[]>([]);
  const [members, setMembers] = useState<FamilyMemberResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters. `undefined` means "all".
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [memberFilter, setMemberFilter] = useState<string | undefined>(undefined);
  const [priorityFilter, setPriorityFilter] = useState<number | undefined>(undefined);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<TaskResponse | null>(null);

  const filter = useMemo<TaskFilter>(
    () => ({
      status: statusFilter,
      assignedMemberId: memberFilter,
      priority: priorityFilter,
    }),
    [statusFilter, memberFilter, priorityFilter],
  );

  const memberName = useCallback(
    (memberId: string | null): string | null => {
      if (!memberId) {
        return null;
      }
      const member = members.find((m) => m.id === memberId);
      return member ? `${member.firstName} ${member.lastName}` : null;
    },
    [members],
  );

  const loadTasks = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setTasks(await taskService.list(familyId, filter));
    } catch (err) {
      setError(getApiErrorMessage(err, t('tasks.errors.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [familyId, filter, t]);

  useEffect(() => {
    void loadTasks();
  }, [loadTasks]);

  // Members are needed for the assignee filter, name lookup and the dialog.
  useEffect(() => {
    if (!familyId) {
      return;
    }
    familyService
      .getMembers(familyId)
      .then(setMembers)
      .catch(() => setMembers([]));
  }, [familyId]);

  const openCreate = () => {
    setEditingTask(null);
    setDialogOpen(true);
  };

  const openEdit = (task: TaskResponse) => {
    setEditingTask(task);
    setDialogOpen(true);
  };

  const handleStatusChange = async (task: TaskResponse, status: number) => {
    try {
      await taskService.updateStatus(familyId, task.id, status);
      await loadTasks();
    } catch (err) {
      setError(getApiErrorMessage(err, t('tasks.errors.saveFailed')));
    }
  };

  const handleDelete = async (task: TaskResponse) => {
    if (!window.confirm(t('tasks.deleteConfirm'))) {
      return;
    }
    try {
      await taskService.remove(familyId, task.id);
      await loadTasks();
    } catch (err) {
      setError(getApiErrorMessage(err, t('tasks.errors.deleteFailed')));
    }
  };

  const clearFilters = () => {
    setStatusFilter(undefined);
    setMemberFilter(undefined);
    setPriorityFilter(undefined);
  };

  const hasFilters =
    statusFilter !== undefined || memberFilter !== undefined || priorityFilter !== undefined;

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-5xl">
      <header className="mb-6 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('nav.tasks')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('pages.tasks.subtitle')}</p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
        >
          {t('tasks.add')}
        </button>
      </header>

      {/* Filters */}
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <select
          value={statusFilter ?? ''}
          onChange={(e) => setStatusFilter(e.target.value === '' ? undefined : Number(e.target.value))}
          className={selectClass}
        >
          <option value="">{t('tasks.filters.allStatuses')}</option>
          {STATUS_VALUES.map((value) => (
            <option key={value} value={value}>
              {t(taskStatusKey(value))}
            </option>
          ))}
        </select>

        <select
          value={memberFilter ?? ''}
          onChange={(e) => setMemberFilter(e.target.value === '' ? undefined : e.target.value)}
          className={selectClass}
        >
          <option value="">{t('tasks.filters.allMembers')}</option>
          {members.map((member) => (
            <option key={member.id} value={member.id}>
              {member.firstName} {member.lastName}
            </option>
          ))}
        </select>

        <select
          value={priorityFilter ?? ''}
          onChange={(e) => setPriorityFilter(e.target.value === '' ? undefined : Number(e.target.value))}
          className={selectClass}
        >
          <option value="">{t('tasks.filters.allPriorities')}</option>
          {PRIORITY_VALUES.map((value) => (
            <option key={value} value={value}>
              {t(priorityKey(value))}
            </option>
          ))}
        </select>

        {hasFilters && (
          <button
            type="button"
            onClick={clearFilters}
            className="text-sm font-medium text-brand-600 hover:underline"
          >
            {t('tasks.filters.clear')}
          </button>
        )}
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {loading ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : tasks.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {hasFilters ? t('tasks.emptyFiltered') : t('tasks.empty')}
        </div>
      ) : (
        <>
          {/* Mobile cards */}
          <div className="space-y-3 sm:hidden">
            {tasks.map((task) => (
              <div key={task.id} className="rounded-xl border border-gray-200 bg-white p-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="font-medium text-gray-900">{task.title}</p>
                    {task.description && (
                      <p className="mt-0.5 line-clamp-2 text-sm text-gray-500">{task.description}</p>
                    )}
                  </div>
                  <span
                    className={`shrink-0 rounded-full px-2 py-0.5 text-xs font-medium ${priorityClasses(task.priority)}`}
                  >
                    {t(priorityKey(task.priority))}
                  </span>
                </div>

                <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-500">
                  <span>{memberName(task.assignedToMemberId) ?? t('tasks.filters.unassigned')}</span>
                  {task.dueDate && (
                    <span>{t('tasks.due', { date: formatDate(task.dueDate, i18n.language) })}</span>
                  )}
                </div>

                <div className="mt-3 flex items-center justify-between gap-3">
                  <select
                    value={task.status}
                    onChange={(e) => handleStatusChange(task, Number(e.target.value))}
                    className={`rounded-full border-0 px-2.5 py-1 text-xs font-medium ${taskStatusClasses(task.status)}`}
                  >
                    {STATUS_VALUES.map((value) => (
                      <option key={value} value={value}>
                        {t(taskStatusKey(value))}
                      </option>
                    ))}
                  </select>
                  <div className="flex items-center gap-3">
                    <button
                      type="button"
                      onClick={() => openEdit(task)}
                      className="text-xs font-medium text-brand-600 hover:underline"
                    >
                      {t('common.edit')}
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(task)}
                      className="text-xs font-medium text-red-600 hover:underline"
                    >
                      {t('common.delete')}
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop table */}
          <div className="hidden overflow-hidden rounded-xl border border-gray-200 bg-white sm:block">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-gray-100 bg-gray-50 text-xs font-medium uppercase tracking-wide text-gray-500">
                <tr>
                  <th className="px-4 py-3">{t('tasks.table.task')}</th>
                  <th className="px-4 py-3">{t('tasks.table.assignee')}</th>
                  <th className="px-4 py-3">{t('tasks.table.dueDate')}</th>
                  <th className="px-4 py-3">{t('tasks.table.priority')}</th>
                  <th className="px-4 py-3">{t('tasks.table.status')}</th>
                  <th className="px-4 py-3 text-right">{t('tasks.table.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {tasks.map((task) => (
                  <tr key={task.id} className="align-top">
                    <td className="px-4 py-3">
                      <p className="font-medium text-gray-900">{task.title}</p>
                      {task.description && (
                        <p className="mt-0.5 line-clamp-1 text-xs text-gray-500">
                          {task.description}
                        </p>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {memberName(task.assignedToMemberId) ?? (
                        <span className="text-gray-400">{t('tasks.filters.unassigned')}</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {task.dueDate ? formatDate(task.dueDate, i18n.language) : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${priorityClasses(task.priority)}`}
                      >
                        {t(priorityKey(task.priority))}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <select
                        value={task.status}
                        onChange={(e) => handleStatusChange(task, Number(e.target.value))}
                        className={`rounded-full border-0 px-2.5 py-1 text-xs font-medium ${taskStatusClasses(task.status)}`}
                      >
                        {STATUS_VALUES.map((value) => (
                          <option key={value} value={value}>
                            {t(taskStatusKey(value))}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-3">
                        <button
                          type="button"
                          onClick={() => openEdit(task)}
                          className="text-xs font-medium text-brand-600 hover:underline"
                        >
                          {t('common.edit')}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(task)}
                          className="text-xs font-medium text-red-600 hover:underline"
                        >
                          {t('common.delete')}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      <TaskDialog
        open={dialogOpen}
        familyId={familyId}
        task={editingTask}
        members={members}
        onClose={() => setDialogOpen(false)}
        onSaved={loadTasks}
      />
    </div>
  );
}
