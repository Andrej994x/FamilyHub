import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from './Modal';
import { shoppingService } from '../services/shoppingService';
import { getApiErrorMessage } from '../utils/apiError';
import type { ShoppingListResponse } from '../types';

interface ShoppingListDialogProps {
  open: boolean;
  familyId: string;
  onClose: () => void;
  onCreated: (list: ShoppingListResponse) => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

export function ShoppingListDialog({ open, familyId, onClose, onCreated }: ShoppingListDialogProps) {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setName('');
      setError(null);
    }
  }, [open]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!name.trim()) {
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const list = await shoppingService.createList(familyId, { name: name.trim() });
      onCreated(list);
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('shopping.errors.createListFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={open} onClose={onClose} title={t('shopping.dialog.addListTitle')}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {t('shopping.dialog.listName')}
          </label>
          <input
            type="text"
            required
            autoFocus
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder={t('shopping.dialog.listNamePlaceholder')}
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
            {submitting ? t('common.saving') : t('shopping.dialog.createList')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
