import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from './Modal';
import { pickupService } from '../services/pickupService';
import { getApiErrorMessage } from '../utils/apiError';
import { toDateTimeLocalInput } from '../utils/date';
import type { ChildResponse, FamilyMemberResponse, PickupResponse } from '../types';

interface PickupDialogProps {
  open: boolean;
  familyId: string;
  pickup: PickupResponse | null;
  members: FamilyMemberResponse[];
  childProfiles: ChildResponse[];
  onClose: () => void;
  onSaved: () => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

/** A sensible default: 3:00 PM today, when creating a new pickup. */
function defaultDateTime(): string {
  const base = new Date();
  base.setHours(15, 0, 0, 0);
  return toDateTimeLocalInput(base.toISOString());
}

export function PickupDialog({
  open,
  familyId,
  pickup,
  members,
  childProfiles,
  onClose,
  onSaved,
}: PickupDialogProps) {
  const { t } = useTranslation();
  const [childProfileId, setChildProfileId] = useState('');
  const [assignedMemberId, setAssignedMemberId] = useState('');
  const [pickupDateTime, setPickupDateTime] = useState('');
  const [location, setLocation] = useState('');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sync form fields whenever the dialog opens or the target pickup changes.
  useEffect(() => {
    if (open) {
      setChildProfileId(pickup?.childProfileId ?? '');
      setAssignedMemberId(pickup?.assignedMemberId ?? '');
      setPickupDateTime(pickup ? toDateTimeLocalInput(pickup.pickupDateTime) : defaultDateTime());
      setLocation(pickup?.location ?? '');
      setNotes(pickup?.notes ?? '');
      setError(null);
    }
  }, [open, pickup]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!childProfileId || !assignedMemberId || pickupDateTime === '' || !location.trim()) {
      return;
    }
    setSubmitting(true);
    setError(null);

    const payload = {
      childProfileId,
      assignedMemberId,
      pickupDateTime: new Date(pickupDateTime).toISOString(),
      location: location.trim(),
      notes: notes.trim() === '' ? null : notes.trim(),
    };

    try {
      if (pickup) {
        await pickupService.update(familyId, pickup.id, payload);
      } else {
        await pickupService.create(familyId, payload);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('pickups.errors.saveFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={pickup ? t('pickups.dialog.editTitle') : t('pickups.dialog.addTitle')}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('pickups.dialog.child')}
            </label>
            <select
              required
              value={childProfileId}
              onChange={(e) => setChildProfileId(e.target.value)}
              className={inputClass}
            >
              <option value="" disabled>
                {t('pickups.dialog.selectChild')}
              </option>
              {childProfiles.map((child) => (
                <option key={child.id} value={child.id}>
                  {child.firstName} {child.lastName}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('pickups.dialog.assignee')}
            </label>
            <select
              required
              value={assignedMemberId}
              onChange={(e) => setAssignedMemberId(e.target.value)}
              className={inputClass}
            >
              <option value="" disabled>
                {t('pickups.dialog.selectMember')}
              </option>
              {members.map((member) => (
                <option key={member.id} value={member.id}>
                  {member.firstName} {member.lastName}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('pickups.dialog.dateTime')}
          </label>
          <input
            type="datetime-local"
            required
            value={pickupDateTime}
            onChange={(e) => setPickupDateTime(e.target.value)}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('pickups.dialog.location')}
          </label>
          <input
            type="text"
            required
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            placeholder={t('pickups.dialog.locationPlaceholder')}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('pickups.dialog.notes')}{' '}
            <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
          </label>
          <textarea
            rows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            className={inputClass}
          />
        </div>

        {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

        {childProfiles.length === 0 && (
          <p className="text-xs text-amber-600">{t('pickups.dialog.noChildrenHint')}</p>
        )}

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
            {submitting ? t('common.saving') : t('pickups.dialog.submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
