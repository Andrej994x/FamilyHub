import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from './Modal';
import { taskService } from '../services/taskService';
import { getApiErrorMessage } from '../utils/apiError';
import { priorityKey } from '../utils/labels';
import { TaskPriority } from '../types';
import type { FamilyMemberResponse, TaskResponse } from '../types';

interface TaskDialogProps {
  open: boolean;
  familyId: string;
  task: TaskResponse | null;
  members: FamilyMemberResponse[];
  onClose: () => void;
  onSaved: () => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

const PRIORITY_VALUES = [TaskPriority.Low, TaskPriority.Medium, TaskPriority.High];

/** ISO string → yyyy-MM-dd for a native date input. */
function toDateInput(value: string | null): string {
  return value ? value.slice(0, 10) : '';
}

export function TaskDialog({ open, familyId, task, members, onClose, onSaved }: TaskDialogProps) {
  const { t } = useTranslation();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [assignedToMemberId, setAssignedToMemberId] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [priority, setPriority] = useState<number>(TaskPriority.Medium);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sync form fields whenever the dialog opens or the target task changes.
  useEffect(() => {
    if (open) {
      setTitle(task?.title ?? '');
      setDescription(task?.description ?? '');
      setAssignedToMemberId(task?.assignedToMemberId ?? '');
      setDueDate(toDateInput(task?.dueDate ?? null));
      setPriority(task?.priority ?? TaskPriority.Medium);
      setError(null);
    }
  }, [open, task]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!title.trim()) {
      return;
    }
    setSubmitting(true);
    setError(null);

    const payload = {
      title: title.trim(),
      description: description.trim() === '' ? null : description.trim(),
      assignedToMemberId: assignedToMemberId === '' ? null : assignedToMemberId,
      dueDate: dueDate === '' ? null : new Date(dueDate).toISOString(),
      priority,
    };

    try {
      if (task) {
        await taskService.update(familyId, task.id, payload);
      } else {
        await taskService.create(familyId, payload);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('tasks.errors.saveFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={task ? t('tasks.dialog.editTitle') : t('tasks.dialog.addTitle')}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('tasks.dialog.title')}
          </label>
          <input
            type="text"
            required
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('tasks.dialog.description')}{' '}
            <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
          </label>
          <textarea
            rows={3}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            className={inputClass}
          />
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('tasks.dialog.assignee')}{' '}
              <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
            </label>
            <select
              value={assignedToMemberId}
              onChange={(event) => setAssignedToMemberId(event.target.value)}
              className={inputClass}
            >
              <option value="">{t('tasks.filters.unassigned')}</option>
              {members.map((member) => (
                <option key={member.id} value={member.id}>
                  {member.firstName} {member.lastName}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('tasks.dialog.priority')}
            </label>
            <select
              value={priority}
              onChange={(event) => setPriority(Number(event.target.value))}
              className={inputClass}
            >
              {PRIORITY_VALUES.map((value) => (
                <option key={value} value={value}>
                  {t(priorityKey(value))}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('tasks.dialog.dueDate')}{' '}
            <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
          </label>
          <input
            type="date"
            value={dueDate}
            onChange={(event) => setDueDate(event.target.value)}
            className={inputClass}
          />
        </div>

        {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

        <div className="flex gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {t('common.cancel')}
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {submitting ? t('common.saving') : t('tasks.dialog.submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
