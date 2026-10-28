import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from '../Modal';
import { getApiErrorMessage } from '../../utils/apiError';

interface CategoryDialogProps {
  open: boolean;
  /** The current (displayed) name when renaming; empty when adding. */
  initialName: string;
  mode: 'add' | 'rename';
  /** Renaming a built-in category replaces its translated label with the typed name. */
  isBuiltIn: boolean;
  onClose: () => void;
  /** Saves the trimmed name; a rejected promise shows its error in the dialog. */
  onSubmit: (name: string) => Promise<void>;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

const MAX_NAME_LENGTH = 50;

export function CategoryDialog({ open, initialName, mode, isBuiltIn, onClose, onSubmit }: CategoryDialogProps) {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const [nameError, setNameError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open) {
      setName(initialName);
      setNameError(null);
      setError(null);
    }
  }, [open, initialName]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const trimmed = name.trim();
    if (trimmed === '' || trimmed.length > MAX_NAME_LENGTH) {
      setNameError(t('expenses.settings.categories.nameInvalid'));
      return;
    }
    // Saving an unchanged name would turn a built-in category into a custom one for no reason.
    if (mode === 'rename' && trimmed === initialName.trim()) {
      onClose();
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await onSubmit(trimmed);
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('expenses.settings.errors.categorySaveFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={mode === 'add' ? t('expenses.settings.categories.addTitle') : t('expenses.settings.categories.renameTitle')}
    >
      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        <div>
          <label htmlFor="category-name" className="mb-1 block text-sm font-medium text-gray-700">
            {t('expenses.settings.categories.name')}
          </label>
          <input
            id="category-name"
            type="text"
            autoComplete="off"
            maxLength={MAX_NAME_LENGTH + 10}
            value={name}
            onChange={(e) => setName(e.target.value)}
            aria-invalid={Boolean(nameError)}
            aria-describedby={nameError ? 'category-name-error' : undefined}
            className={inputClass}
          />
          {nameError && (
            <p id="category-name-error" className="mt-1 text-xs text-red-600">
              {nameError}
            </p>
          )}
          {mode === 'rename' && isBuiltIn && (
            <p className="mt-1 text-xs text-gray-500">{t('expenses.settings.categories.builtInHint')}</p>
          )}
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
            {submitting ? t('common.saving') : t('expenses.settings.categories.submit')}
          </button>
        </div>
      </form>
    </Modal>
  );
}
