import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from './Modal';
import { childService } from '../services/childService';
import { getApiErrorMessage } from '../utils/apiError';
import type { ChildResponse } from '../types';

interface ChildDialogProps {
  open: boolean;
  familyId: string;
  child: ChildResponse | null;
  onClose: () => void;
  onSaved: () => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

export function ChildDialog({ open, familyId, child, onClose, onSaved }: ChildDialogProps) {
  const { t } = useTranslation();
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sync form fields whenever the dialog opens or the target child changes.
  useEffect(() => {
    if (open) {
      setFirstName(child?.firstName ?? '');
      setLastName(child?.lastName ?? '');
      setDateOfBirth(child?.dateOfBirth ?? '');
      setNotes(child?.notes ?? '');
      setError(null);
    }
  }, [open, child]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!firstName.trim() || !lastName.trim()) {
      return;
    }
    setSubmitting(true);
    setError(null);

    const payload = {
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      dateOfBirth: dateOfBirth === '' ? null : dateOfBirth,
      notes: notes.trim() === '' ? null : notes.trim(),
    };

    try {
      if (child) {
        await childService.update(familyId, child.id, payload);
      } else {
        await childService.create(familyId, payload);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('children.errors.saveFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={child ? t('children.dialog.editTitle') : t('children.dialog.addTitle')}
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('children.dialog.firstName')}
            </label>
            <input
              type="text"
              required
              value={firstName}
              onChange={(event) => setFirstName(event.target.value)}
              className={inputClass}
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('children.dialog.lastName')}
            </label>
            <input
              type="text"
              required
              value={lastName}
              onChange={(event) => setLastName(event.target.value)}
              className={inputClass}
            />
          </div>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('children.dialog.dob')}{' '}
            <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
          </label>
          <input
            type="date"
            value={dateOfBirth}
            onChange={(event) => setDateOfBirth(event.target.value)}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('children.dialog.notes')}{' '}
            <span className="text-xs font-normal text-gray-400">({t('common.optional')})</span>
          </label>
          <textarea
            rows={3}
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            className={inputClass}
          />
          <p className="mt-1 text-xs text-amber-600">{t('children.notesWarning')}</p>
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
            {submitting ? t('common.saving') : t('children.dialog.submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
