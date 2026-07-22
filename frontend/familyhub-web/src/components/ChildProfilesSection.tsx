import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { childService } from '../services/childService';
import { getApiErrorMessage } from '../utils/apiError';
import { formatDate } from '../utils/format';
import { ChildDialog } from './ChildDialog';
import type { ChildResponse } from '../types';

interface ChildProfilesSectionProps {
  familyId: string;
  canManage: boolean;
}

export function ChildProfilesSection({ familyId, canManage }: ChildProfilesSectionProps) {
  const { t, i18n } = useTranslation();
  const [children, setChildren] = useState<ChildResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dialog, setDialog] = useState<{ open: boolean; child: ChildResponse | null }>({
    open: false,
    child: null,
  });

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setChildren(await childService.list(familyId));
    } catch (err) {
      setError(getApiErrorMessage(err, t('children.errors.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [familyId, t]);

  useEffect(() => {
    void load();
  }, [load]);

  const handleDelete = async (child: ChildResponse) => {
    if (!window.confirm(t('children.deleteConfirm'))) {
      return;
    }
    try {
      await childService.remove(familyId, child.id);
      await load();
    } catch (err) {
      setError(getApiErrorMessage(err, t('children.errors.deleteFailed')));
    }
  };

  return (
    <section className="mt-8">
      <div className="mb-3 flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">{t('children.title')}</h2>
          <p className="text-sm text-gray-500">{t('children.subtitle')}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={() => setDialog({ open: true, child: null })}
            className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
          >
            {t('children.add')}
          </button>
        )}
      </div>

      {error && (
        <div className="mb-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {loading ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : children.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-8 text-center text-sm text-gray-400">
          {t('children.empty')}
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {children.map((child) => (
            <div key={child.id} className="rounded-xl border border-gray-200 bg-white p-4">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="truncate font-medium text-gray-900">
                    {child.firstName} {child.lastName}
                  </p>
                  {child.dateOfBirth && (
                    <p className="mt-0.5 text-xs text-gray-500">
                      {t('children.born', { date: formatDate(child.dateOfBirth, i18n.language) })}
                    </p>
                  )}
                  {child.notes && <p className="mt-2 text-sm text-gray-600">{child.notes}</p>}
                </div>
                {canManage && (
                  <div className="flex shrink-0 gap-1">
                    <button
                      type="button"
                      onClick={() => setDialog({ open: true, child })}
                      className="rounded-lg px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50"
                    >
                      {t('common.edit')}
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(child)}
                      className="rounded-lg px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50"
                    >
                      {t('common.delete')}
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <ChildDialog
        open={dialog.open}
        familyId={familyId}
        child={dialog.child}
        onClose={() => setDialog({ open: false, child: null })}
        onSaved={load}
      />
    </section>
  );
}
