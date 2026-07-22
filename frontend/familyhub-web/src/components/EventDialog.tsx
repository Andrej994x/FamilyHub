import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from './Modal';
import { eventService } from '../services/eventService';
import { getApiErrorMessage } from '../utils/apiError';
import { toDateTimeLocalInput } from '../utils/date';
import { eventTypeKey } from '../utils/labels';
import { EventType } from '../types';
import type { ChildResponse, EventResponse, FamilyMemberResponse } from '../types';

interface EventDialogProps {
  open: boolean;
  familyId: string;
  event: EventResponse | null;
  members: FamilyMemberResponse[];
  childProfiles: ChildResponse[];
  /** Local midnight of the day to prefill when creating a new event. */
  defaultDate: Date | null;
  onClose: () => void;
  onSaved: () => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

const EVENT_TYPE_VALUES = [
  EventType.Kindergarten,
  EventType.School,
  EventType.Doctor,
  EventType.Training,
  EventType.Birthday,
  EventType.Family,
  EventType.Other,
];

/** A sensible default start: 9:00 AM on the given day (or today). */
function defaultStart(day: Date | null): string {
  const base = day ? new Date(day) : new Date();
  base.setHours(9, 0, 0, 0);
  return toDateTimeLocalInput(base.toISOString());
}

export function EventDialog({
  open,
  familyId,
  event,
  members,
  childProfiles,
  defaultDate,
  onClose,
  onSaved,
}: EventDialogProps) {
  const { t } = useTranslation();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [eventType, setEventType] = useState<number>(EventType.Family);
  const [startDateTime, setStartDateTime] = useState('');
  const [endDateTime, setEndDateTime] = useState('');
  const [location, setLocation] = useState('');
  const [assignedMemberId, setAssignedMemberId] = useState('');
  const [childProfileId, setChildProfileId] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sync form fields whenever the dialog opens or the target event changes.
  useEffect(() => {
    if (open) {
      setTitle(event?.title ?? '');
      setDescription(event?.description ?? '');
      setEventType(event?.eventType ?? EventType.Family);
      setStartDateTime(event ? toDateTimeLocalInput(event.startDateTime) : defaultStart(defaultDate));
      setEndDateTime(event?.endDateTime ? toDateTimeLocalInput(event.endDateTime) : '');
      setLocation(event?.location ?? '');
      setAssignedMemberId(event?.assignedMemberId ?? '');
      setChildProfileId(event?.childProfileId ?? '');
      setError(null);
    }
  }, [open, event, defaultDate]);

  const handleSubmit = async (formEvent: React.FormEvent) => {
    formEvent.preventDefault();
    if (!title.trim() || startDateTime === '') {
      return;
    }
    if (endDateTime !== '' && new Date(endDateTime) <= new Date(startDateTime)) {
      setError(t('calendar.errors.endBeforeStart'));
      return;
    }
    setSubmitting(true);
    setError(null);

    const payload = {
      title: title.trim(),
      description: description.trim() === '' ? null : description.trim(),
      eventType,
      startDateTime: new Date(startDateTime).toISOString(),
      endDateTime: endDateTime === '' ? null : new Date(endDateTime).toISOString(),
      location: location.trim() === '' ? null : location.trim(),
      assignedMemberId: assignedMemberId === '' ? null : assignedMemberId,
      childProfileId: childProfileId === '' ? null : childProfileId,
    };

    try {
      if (event) {
        await eventService.update(familyId, event.id, payload);
      } else {
        await eventService.create(familyId, payload);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('calendar.errors.saveFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={event ? t('calendar.dialog.editTitle') : t('calendar.dialog.addTitle')}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('calendar.dialog.title')}
          </label>
          <input
            type="text"
            required
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('calendar.dialog.type')}
          </label>
          <select
            value={eventType}
            onChange={(e) => setEventType(Number(e.target.value))}
            className={inputClass}
          >
            {EVENT_TYPE_VALUES.map((value) => (
              <option key={value} value={value}>
                {t(eventTypeKey(value))}
              </option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('calendar.dialog.start')}
            </label>
            <input
              type="datetime-local"
              required
              value={startDateTime}
              onChange={(e) => setStartDateTime(e.target.value)}
              className={inputClass}
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('calendar.dialog.end')}{' '}
              <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
            </label>
            <input
              type="datetime-local"
              value={endDateTime}
              min={startDateTime || undefined}
              onChange={(e) => setEndDateTime(e.target.value)}
              className={inputClass}
            />
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('calendar.dialog.location')}{' '}
            <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
          </label>
          <input
            type="text"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            className={inputClass}
          />
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('calendar.dialog.assignee')}{' '}
              <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
            </label>
            <select
              value={assignedMemberId}
              onChange={(e) => setAssignedMemberId(e.target.value)}
              className={inputClass}
            >
              <option value="">{t('calendar.dialog.noOne')}</option>
              {members.map((member) => (
                <option key={member.id} value={member.id}>
                  {member.firstName} {member.lastName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('calendar.dialog.child')}{' '}
              <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
            </label>
            <select
              value={childProfileId}
              onChange={(e) => setChildProfileId(e.target.value)}
              className={inputClass}
            >
              <option value="">{t('calendar.dialog.noChild')}</option>
              {childProfiles.map((child) => (
                <option key={child.id} value={child.id}>
                  {child.firstName} {child.lastName}
                </option>
              ))}
            </select>
          </div>
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
            {submitting ? t('common.saving') : t('calendar.dialog.submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
