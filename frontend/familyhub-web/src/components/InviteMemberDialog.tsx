import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from './Modal';
import { invitationService } from '../services/invitationService';
import { getApiErrorMessage } from '../utils/apiError';
import { FamilyRole } from '../types';
import type { CreatedInvitationResponse } from '../types';

interface InviteMemberDialogProps {
  open: boolean;
  familyId: string;
  onClose: () => void;
  onInvited: () => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

export function InviteMemberDialog({ open, familyId, onClose, onInvited }: InviteMemberDialogProps) {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<number>(FamilyRole.Member);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<CreatedInvitationResponse | null>(null);
  const [copied, setCopied] = useState<'token' | 'link' | null>(null);

  const reset = () => {
    setEmail('');
    setRole(FamilyRole.Member);
    setSubmitting(false);
    setError(null);
    setCreated(null);
    setCopied(null);
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!email.trim()) {
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const result = await invitationService.create(familyId, { email: email.trim(), role });
      setCreated(result);
      onInvited();
    } catch (err) {
      setError(getApiErrorMessage(err, t('invitations.errors.createFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  const copy = async (value: string, which: 'token' | 'link') => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(which);
      window.setTimeout(() => setCopied(null), 1500);
    } catch {
      // Clipboard may be unavailable; silently ignore.
    }
  };

  return (
    <Modal
      open={open}
      onClose={handleClose}
      title={created ? t('invitations.dialog.successTitle') : t('invitations.dialog.title')}
    >
      {created ? (
        <div className="space-y-4">
          <p className="text-sm text-gray-500">{t('invitations.dialog.successHint')}</p>

          <div>
            <label className="mb-1 block text-xs font-medium text-gray-500">
              {t('invitations.dialog.link')}
            </label>
            <div className="flex gap-2">
              <input readOnly value={created.acceptUrl} className={inputClass} />
              <button
                type="button"
                onClick={() => copy(created.acceptUrl, 'link')}
                className="shrink-0 rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
              >
                {copied === 'link' ? t('common.copied') : t('common.copy')}
              </button>
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-gray-500">
              {t('invitations.dialog.token')}
            </label>
            <div className="flex gap-2">
              <input readOnly value={created.token} className={`${inputClass} font-mono text-xs`} />
              <button
                type="button"
                onClick={() => copy(created.token, 'token')}
                className="shrink-0 rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
              >
                {copied === 'token' ? t('common.copied') : t('common.copy')}
              </button>
            </div>
          </div>

          <button
            type="button"
            onClick={handleClose}
            className="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
          >
            {t('invitations.dialog.done')}
          </button>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('invitations.dialog.email')}
            </label>
            <input
              type="email"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className={inputClass}
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t('invitations.dialog.role')}
            </label>
            <select
              value={role}
              onChange={(event) => setRole(Number(event.target.value))}
              className={inputClass}
            >
              <option value={FamilyRole.Parent}>{t('roles.parent')}</option>
              <option value={FamilyRole.Member}>{t('roles.member')}</option>
              <option value={FamilyRole.Child}>{t('roles.child')}</option>
            </select>
          </div>

          {error && (
            <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="w-full rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {submitting ? t('common.saving') : t('invitations.dialog.submit')}
          </button>
        </form>
      )}
    </Modal>
  );
}
